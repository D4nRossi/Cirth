using Cirth.Application.Common.Ports;
using Cirth.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cirth.Application.Features.Tags.SuggestTags;

public sealed record SuggestTagsCommand(Guid DocumentId) : IRequest<Result<IReadOnlyList<string>>>;

internal sealed class SuggestTagsCommandHandler(
    ILlmChatService llmService,
    IQueryDbContext db) : IRequestHandler<SuggestTagsCommand, Result<IReadOnlyList<string>>>
{
    public async Task<Result<IReadOnlyList<string>>> Handle(SuggestTagsCommand cmd, CancellationToken ct)
    {
        var docId = new DocumentId(cmd.DocumentId);

        var doc = await db.Documents
            .Where(d => d.Id == docId)
            .FirstOrDefaultAsync(ct);
        if (doc is null)
            return Error.NotFound("document.not_found", "Document not found.");

        if (doc.CurrentVersionId is null)
            return Result<IReadOnlyList<string>>.Success([]);

        var versionId = doc.CurrentVersionId.Value;
        var sample = await db.Chunks
            .Where(c => c.DocumentVersionId == versionId && c.IsCurrent)
            .OrderBy(c => c.Ordinal)
            .Take(3)
            .Select(c => c.Content)
            .ToListAsync(ct);

        if (sample.Count == 0)
            return Result<IReadOnlyList<string>>.Success([]);

        var sampleText = string.Join("\n\n", sample);
        var systemPrompt = "You suggest 3-5 concise tags for documents. Return only a comma-separated list of tags, lowercase, no punctuation.";
        var userPrompt = $"Document title: {doc.Title}\n\nContent sample:\n{sampleText[..Math.Min(2000, sampleText.Length)]}";

        var response = await llmService.CompleteChatAsync(systemPrompt, userPrompt, "gpt-4.1-mini", ct);
        var tags = response.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(5).ToList();

        return Result<IReadOnlyList<string>>.Success(tags);
    }
}
