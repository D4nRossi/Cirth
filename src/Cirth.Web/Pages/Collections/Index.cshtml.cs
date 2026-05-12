using Cirth.Application.Features.Collections.CreateCollection;
using Cirth.Application.Features.Collections.ListCollections;
using Cirth.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Cirth.Web.Pages.Collections;

[Authorize]
public sealed class IndexModel(IMediator mediator) : CirthPageModel(mediator)
{
    public IReadOnlyList<CollectionSummaryDto> Collections { get; private set; } = [];

    [BindProperty]
    public NewCollectionInput NewCollection { get; set; } = new();

    public async Task OnGetAsync(CancellationToken ct)
    {
        Collections = await Mediator.Send(new ListCollectionsQuery(), ct) is { IsSuccess: true, Value: { } v } ? v : [];
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken ct)
    {
        if (ModelState.IsValid && !string.IsNullOrWhiteSpace(NewCollection.Name))
        {
            var result = await Mediator.Send(
                new CreateCollectionCommand(NewCollection.Name.Trim(), NewCollection.Description?.Trim().NullIfEmpty()),
                ct);
            if (result.IsSuccess)
            {
                Toast("Coleção criada.", ToastLevel.Success);
                NewCollection = new();
                ModelState.Clear();
            }
            else
            {
                Toast(result.Error!.Message, ToastLevel.Error);
            }
        }

        Collections = await Mediator.Send(new ListCollectionsQuery(), ct) is { IsSuccess: true, Value: { } v } ? v : [];
        return Partial("_CollectionList", Collections);
    }

    public sealed class NewCollectionInput
    {
        [Required] [StringLength(200)] public string Name { get; set; } = string.Empty;
        [StringLength(500)] public string? Description { get; set; }
    }
}
