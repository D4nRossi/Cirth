using Cirth.Application.Features.Admin.GetSystemStatus;
using Cirth.Application.Features.Admin.RetryFailedJobs;
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

    public async Task<IActionResult> OnPostRetryFailedJobsAsync(CancellationToken ct)
    {
        try
        {
            var retry = await Mediator.Send(new RetryFailedJobsCommand(), ct);
            if (retry.IsSuccess)
                Toast($"{retry.Value} job(s) reenviado(s) para a fila.", ToastLevel.Success);
            else
                Toast(retry.Error!.Message, ToastLevel.Error);
        }
        catch (Exception ex)
        {
            Toast($"Erro ao reprocessar: {ex.Message}", ToastLevel.Error);
        }

        // Refresh the status panel after the retry so the user sees the current state.
        var status = await Mediator.Send(new GetSystemStatusQuery(), ct);
        return Partial("_Status", status.IsSuccess ? status.Value : null);
    }
}
