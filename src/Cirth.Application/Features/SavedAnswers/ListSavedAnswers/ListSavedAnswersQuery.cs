using Cirth.Application.Common.Ports;
using Cirth.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cirth.Application.Features.SavedAnswers.ListSavedAnswers;

public sealed record ListSavedAnswersQuery(string? SearchQuery = null) : IRequest<Result<IReadOnlyList<SavedAnswerSummaryDto>>>;

public sealed record SavedAnswerSummaryDto(
    Guid Id, string Question, string Answer, int UsageCount, int UtilityScore, DateTimeOffset CreatedAt);

internal sealed class ListSavedAnswersQueryHandler(
    ITenantProvider tenantProvider,
    IQueryDbContext db,
    IEmbeddingService embeddingService,
    IVectorStore vectorStore) : IRequestHandler<ListSavedAnswersQuery, Result<IReadOnlyList<SavedAnswerSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<SavedAnswerSummaryDto>>> Handle(ListSavedAnswersQuery q, CancellationToken ct)
    {
        var tenantId = tenantProvider.CurrentTenantId;
        IReadOnlyList<SavedAnswerSummaryDto> result;

        if (!string.IsNullOrWhiteSpace(q.SearchQuery))
        {
            var embedding = await embeddingService.EmbedAsync(q.SearchQuery, ct);
            var hits = await vectorStore.SearchAsync(tenantId, embedding, 20, ct);
            var ids = hits.Select(h => h.ChunkId.Value).ToList();

            result = await db.SavedAnswers
                .Where(s => s.TenantId.Value == tenantId.Value && ids.Contains(s.Id.Value))
                .Select(s => new SavedAnswerSummaryDto(s.Id.Value, s.Question, s.Answer, s.UsageCount, s.UtilityScore, s.CreatedAt))
                .ToListAsync(ct);
        }
        else
        {
            result = await db.SavedAnswers
                .Where(s => s.TenantId.Value == tenantId.Value)
                .OrderByDescending(s => s.UsageCount)
                .Take(50)
                .Select(s => new SavedAnswerSummaryDto(s.Id.Value, s.Question, s.Answer, s.UsageCount, s.UtilityScore, s.CreatedAt))
                .ToListAsync(ct);
        }

        return Result<IReadOnlyList<SavedAnswerSummaryDto>>.Success(result);
    }
}
