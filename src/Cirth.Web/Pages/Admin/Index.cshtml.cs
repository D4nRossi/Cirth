using Cirth.Application.Features.Admin.GetSystemStatus;
using Cirth.Application.Features.Identity.GetMyProfile;
using Cirth.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cirth.Web.Pages.Admin;

[Authorize(Policy = "Admin")]
public sealed class IndexModel(IMediator mediator) : CirthPageModel(mediator)
{
    public MyProfileDto? Profile { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetMyProfileQuery(), ct);
        if (result.IsSuccess) Profile = result.Value;
        else Toast(result.Error!.Message, ToastLevel.Error);
    }

    public async Task<IActionResult> OnGetStatusAsync(CancellationToken ct)
    {
        try
        {
            var result = await Mediator.Send(new GetSystemStatusQuery(), ct);
            return Partial("_Status", result.IsSuccess ? result.Value : null);
        }
        catch (Exception ex)
        {
            Toast($"Erro ao verificar conexões: {ex.Message}", ToastLevel.Error);
            return Partial("_Status", (SystemStatusDto?)null);
        }
    }
}
