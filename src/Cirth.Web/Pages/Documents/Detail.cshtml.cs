using Cirth.Application.Features.Documents.GetDocument;
using Cirth.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace Cirth.Web.Pages.Documents;

[Authorize]
public sealed class DetailModel(IMediator mediator) : CirthPageModel(mediator)
{
    public DocumentDetailDto? Doc { get; private set; }
    public string? Error { get; private set; }

    public async Task OnGetAsync(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await Mediator.Send(new GetDocumentQuery(id), ct);
            if (result.IsSuccess) Doc = result.Value;
            else Error = result.Error!.Message;
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
    }
}
