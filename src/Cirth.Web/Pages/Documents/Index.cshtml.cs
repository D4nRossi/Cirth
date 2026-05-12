using Cirth.Application.Features.Documents.ListDocuments;
using Cirth.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cirth.Web.Pages.Documents;

[Authorize]
public sealed class IndexModel(IMediator mediator) : CirthPageModel(mediator)
{
    public IReadOnlyList<DocumentSummaryDto> Documents { get; private set; } = [];

    [BindProperty(SupportsGet = true, Name = "q")]
    public string? Filter { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadAsync(ct);
    }

    public async Task<IActionResult> OnGetFilterAsync(CancellationToken ct)
    {
        await LoadAsync(ct);
        return Partial("_DocumentTable", Documents);
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var result = await Mediator.Send(new ListDocumentsQuery(PageSize: 100), ct);
        if (!result.IsSuccess)
        {
            Toast(result.Error!.Message, ToastLevel.Error);
            return;
        }

        var items = result.Value!.Items;
        if (!string.IsNullOrWhiteSpace(Filter))
        {
            items = items.Where(d => d.Title.Contains(Filter, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        Documents = items;
    }
}
