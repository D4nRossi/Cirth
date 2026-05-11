using Cirth.Application.Common.Ports;
using Cirth.Domain.SavedAnswers;
using Cirth.Shared;
using FluentValidation;
using MediatR;

namespace Cirth.Application.Features.SavedAnswers.CreateSavedAnswer;

public sealed record CreateSavedAnswerCommand(
    Guid ConversationId,
    Guid AssistantMessageId,
    string Question,
    string Answer,
    Guid[] CitedChunkIds) : IRequest<Result<SavedAnswerDto>>;

public sealed record SavedAnswerDto(Guid Id, string Question, string Answer, DateTimeOffset CreatedAt);

public sealed class CreateSavedAnswerCommandValidator : AbstractValidator<CreateSavedAnswerCommand>
{
    public CreateSavedAnswerCommandValidator()
    {
        RuleFor(x => x.Question).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Answer).NotEmpty();
    }
}

internal sealed class CreateSavedAnswerCommandHandler(
    ITenantProvider tenantProvider,
    ISavedAnswerRepository repo,
    IEmbeddingService embeddingService,
    IVectorStore vectorStore,
    IUnitOfWork uow) : IRequestHandler<CreateSavedAnswerCommand, Result<SavedAnswerDto>>
{
    public async Task<Result<SavedAnswerDto>> Handle(CreateSavedAnswerCommand cmd, CancellationToken ct)
    {
        var tenantId = tenantProvider.CurrentTenantId;
        var qdrantId = Guid.NewGuid();

        var embedding = await embeddingService.EmbedAsync(cmd.Question, ct);

        var savedAnswer = SavedAnswer.Create(tenantId, cmd.Question, cmd.Answer, cmd.CitedChunkIds, qdrantId);
        await repo.AddAsync(savedAnswer, ct);

        // Index question embedding in Qdrant (separate collection "saved_answers")
        await vectorStore.UpsertAsync(
            tenantId,
            new(qdrantId), // reuse ChunkId as the point id
            embedding,
            new Dictionary<string, object>
            {
                ["type"] = "saved_answer",
                ["saved_answer_id"] = savedAnswer.Id.Value.ToString()
            },
            ct);

        await uow.CommitAsync(ct);
        return Result<SavedAnswerDto>.Success(new(savedAnswer.Id.Value, savedAnswer.Question, savedAnswer.Answer, savedAnswer.CreatedAt));
    }
}
