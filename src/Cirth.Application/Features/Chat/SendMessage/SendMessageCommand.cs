using Cirth.Application.Common.Ports;
using Cirth.Application.Features.Search.HybridSearch;
using Cirth.Domain.SavedAnswers;
using Cirth.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cirth.Application.Features.Chat.SendMessage;

public sealed record SendMessageCommand(
    Guid ConversationId,
    string Content,
    bool SkipSavedAnswerCheck = false) : IRequest<Result<IAsyncEnumerable<string>>>;

public sealed class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(4000);
    }
}

public sealed record SendMessageResult(
    Guid MessageId,
    IAsyncEnumerable<string> TokenStream,
    SavedAnswerMatch? SavedAnswerMatch);

public sealed record SavedAnswerMatch(Guid Id, string Question, string Answer);

internal sealed class SendMessageCommandHandler(
    ITenantProvider tenantProvider,
    IConversationRepository convRepo,
    ISavedAnswerRepository savedAnswerRepo,
    IUserQuotaRepository quotaRepo,
    IEmbeddingService embeddingService,
    IVectorStore vectorStore,
    ILlmChatService llmService,
    IMediator mediator,
    IUnitOfWork uow,
    IQueryDbContext db) : IRequestHandler<SendMessageCommand, Result<IAsyncEnumerable<string>>>
{
    private const double SavedAnswerThreshold = 0.85;
    private const int HistoryWindowSize = 10;
    private const string ChatModel = "gpt-4.1";

    public async Task<Result<IAsyncEnumerable<string>>> Handle(SendMessageCommand cmd, CancellationToken ct)
    {
        var tenantId = tenantProvider.CurrentTenantId;
        var userId = tenantProvider.CurrentUserId;

        var conv = await convRepo.GetByIdAsync(new(cmd.ConversationId), ct);
        if (conv is null || conv.TenantId != tenantId)
            return Error.NotFound("conversation.not_found", "Conversation not found.");

        // Quota check — estimate tokens before calling LLM
        var quota = await quotaRepo.GetByUserIdAsync(userId, ct);
        if (quota is not null && !quota.CanConsumeTokens(2000))
            return Error.QuotaExceeded("quota.tokens_exceeded",
                "Daily token limit reached. Try again tomorrow.");

        // SavedAnswer pre-check
        if (!cmd.SkipSavedAnswerCheck)
        {
            var savedMatch = await CheckSavedAnswersAsync(tenantId, cmd.Content, ct);
            if (savedMatch is not null)
            {
                // Return saved answer without calling LLM
                savedMatch.RecordUsage();
                await uow.CommitAsync(ct);
                var savedText = savedMatch.Answer;
                return Result<IAsyncEnumerable<string>>.Success(
                    YieldSavedAnswer(savedText));
            }
        }

        // Hybrid search for context
        var searchResult = await mediator.Send(new HybridSearchQuery(cmd.Content, TopK: 8), ct);
        var hits = searchResult.IsSuccess ? searchResult.Value!.Hits : [];

        // Build prompt
        var systemPrompt = BuildSystemPrompt(hits);

        // Conversation history (sliding window).
        // EF Core can't translate `TakeLast` to SQL — fetch the most recent N descending
        // and reverse in memory. `.ToString().ToLowerInvariant()` on the enum also doesn't
        // translate cleanly inside a projection, so we project to an anonymous shape on
        // the server and finish the conversion client-side.
        var rawHistory = await db.Messages
            .Where(m => m.ConversationId.Value == cmd.ConversationId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(HistoryWindowSize)
            .Select(m => new { m.Role, m.Content })
            .ToListAsync(ct);

        rawHistory.Reverse();
        var history = rawHistory
            .Select(m => new ChatMessageDto(m.Role.ToString()!.ToLowerInvariant(), m.Content))
            .ToList();

        // Persist user message
        conv.AddUserMessage(cmd.Content);

        // Stream LLM response
        var stream = StreamResponseAsync(conv, cmd.Content, systemPrompt, history,
            hits, quota, userId, ct);

        // Commit user message (assistant message committed after stream ends)
        await uow.CommitAsync(ct);

        return Result<IAsyncEnumerable<string>>.Success(stream);
    }

    private async IAsyncEnumerable<string> StreamResponseAsync(
        Domain.Conversations.Conversation conv,
        string userMessage,
        string systemPrompt,
        IReadOnlyList<ChatMessageDto> history,
        IReadOnlyList<SearchHit> hits,
        Domain.Quotas.UserQuota? quota,
        UserId userId,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var fullResponse = new System.Text.StringBuilder();
        int tokensUsed = 0;

        await foreach (var token in llmService.StreamChatAsync(systemPrompt, history, userMessage, ChatModel, ct))
        {
            fullResponse.Append(token);
            yield return token;
        }

        // After stream completes, persist assistant message and update quota
        var citedIds = hits.Select(h => h.ChunkId).ToArray();
        tokensUsed = await llmService.EstimateTokensAsync(fullResponse.ToString(), ct);

        conv.AddAssistantMessage(fullResponse.ToString(), citedIds, tokensUsed, ChatModel);
        quota?.ConsumeTokens(tokensUsed);

        await uow.CommitAsync(ct);
    }

    private async Task<SavedAnswer?> CheckSavedAnswersAsync(Shared.TenantId tenantId, string question, CancellationToken ct)
    {
        var embedding = await embeddingService.EmbedAsync(question, ct);
        var hits = await vectorStore.SearchAsync(tenantId, embedding, 1, ct);
        if (hits.Count == 0 || hits[0].Score < SavedAnswerThreshold)
            return null;

        return await savedAnswerRepo.GetByIdAsync(new(hits[0].ChunkId.Value), ct);
    }

    private static string BuildSystemPrompt(IReadOnlyList<SearchHit> hits)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("""
            You are Cirth, a precise and concise knowledge assistant.
            Answer ONLY based on the provided context below.
            If the answer is not in the context, say so clearly.
            Cite sources inline with [n] notation where n is the chunk number.
            Respond in the same language as the user's question.
            """);

        if (hits.Count > 0)
        {
            sb.AppendLine("\n## Knowledge Context\n");
            for (int i = 0; i < hits.Count; i++)
            {
                sb.AppendLine($"[{i + 1}] **{hits[i].DocumentTitle}** (chunk {hits[i].Ordinal})");
                sb.AppendLine(hits[i].Content);
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private static async IAsyncEnumerable<string> YieldSavedAnswer(string answer)
    {
        // Simulate token-by-token for saved answers so UI behavior is consistent
        foreach (var word in answer.Split(' '))
        {
            yield return word + " ";
            await Task.Yield();
        }
    }
}
