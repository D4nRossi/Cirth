using Cirth.Application.Common.Ports;
using Cirth.Shared;
using MediatR;

namespace Cirth.Application.Features.Tags.ListTags;

public sealed record ListTagsQuery : IRequest<Result<IReadOnlyList<CreateTag.TagDto>>>;

internal sealed class ListTagsQueryHandler(
    ITenantProvider tenantProvider,
    ITagRepository tagRepo) : IRequestHandler<ListTagsQuery, Result<IReadOnlyList<CreateTag.TagDto>>>
{
    public async Task<Result<IReadOnlyList<CreateTag.TagDto>>> Handle(ListTagsQuery q, CancellationToken ct)
    {
        var tags = await tagRepo.ListAsync(tenantProvider.CurrentTenantId, ct);
        var result = tags.Select(t => new CreateTag.TagDto(t.Id.Value, t.Name, t.Color)).ToList();
        return Result<IReadOnlyList<CreateTag.TagDto>>.Success(result);
    }
}
