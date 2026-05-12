using Cirth.Application.Features.Search.HybridSearch;
using Cirth.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cirth.Web.Pages;

[Authorize]
public sealed class SearchModel(IMediator mediator) : CirthPageModel(mediator)
{
    [BindProperty(SupportsGet = true, Name = "q")]
    public string? Query { get; set; }

    public HybridSearchResult? Result { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(Query)) return;
        try
        {
            var result = await Mediator.Send(new HybridSearchQuery(Query), ct);
            if (result.IsSuccess) Result = result.Value;
            else Toast(result.Error!.Message, ToastLevel.Error);
        }
        catch (Exception ex)
        {
            Toast($"Erro na busca: {ex.Message}", ToastLevel.Error);
        }
    }
}
