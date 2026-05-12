using Cirth.Application.Features.SavedAnswers.ListSavedAnswers;
using Cirth.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace Cirth.Web.Pages.Saved;

[Authorize]
public sealed class IndexModel(IMediator mediator) : CirthPageModel(mediator)
{
    public IReadOnlyList<SavedAnswerSummaryDto> Answers { get; private set; } = [];

    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true, Name = "q")]
    public string? Query { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var q = string.IsNullOrWhiteSpace(Query) ? null : Query;
        var result = await Mediator.Send(new ListSavedAnswersQuery(q), ct);
        if (result.IsSuccess) Answers = result.Value!;
        else Toast(result.Error!.Message, ToastLevel.Error);
    }
}
