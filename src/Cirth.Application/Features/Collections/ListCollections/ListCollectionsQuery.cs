using Cirth.Application.Common.Ports;
using Cirth.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cirth.Application.Features.Collections.ListCollections;

public sealed record ListCollectionsQuery : IRequest<Result<IReadOnlyList<CollectionSummaryDto>>>;

public sealed record CollectionSummaryDto(Guid Id, string Name, string? Description, int DocumentCount, DateTimeOffset CreatedAt);

internal sealed class ListCollectionsQueryHandler(
    IQueryDbContext db) : IRequestHandler<ListCollectionsQuery, Result<IReadOnlyList<CollectionSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<CollectionSummaryDto>>> Handle(ListCollectionsQuery q, CancellationToken ct)
    {
        var collections = await db.Collections
            .OrderBy(c => c.Name)
            .Select(c => new CollectionSummaryDto(
                c.Id.Value, c.Name, c.Description,
                db.CollectionDocuments.Count(cd => cd.CollectionId == c.Id),
                c.CreatedAt))
            .ToListAsync(ct);

        return Result<IReadOnlyList<CollectionSummaryDto>>.Success(collections);
    }
}
