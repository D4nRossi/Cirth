using Cirth.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace Cirth.Web.Infrastructure;

/// <summary>
/// Base PageModel com helpers para MediatR + toast (via HX-Trigger ou TempData).
/// </summary>
public abstract class CirthPageModel(IMediator mediator) : PageModel
{
    protected readonly IMediator Mediator = mediator;

    /// <summary>True quando a request veio do HTMX (cabeçalho HX-Request).</summary>
    protected bool IsHtmx => Request.Headers.ContainsKey("HX-Request");

    /// <summary>
    /// Adiciona um toast à próxima resposta. Se HTMX, vai como HX-Trigger; caso contrário, TempData.
    /// </summary>
    protected void Toast(string message, ToastLevel level = ToastLevel.Info)
    {
        var payload = new { toast = new { message, level = level.ToString().ToLowerInvariant() } };
        if (IsHtmx)
        {
            var existing = Response.Headers.TryGetValue("HX-Trigger", out var v) ? v.ToString() : null;
            // If a trigger is already set, merge — last-wins on collision keeps it simple.
            Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(payload);
        }
        else
        {
            TempData["toast.message"] = message;
            TempData["toast.level"] = level.ToString().ToLowerInvariant();
        }
    }

    /// <summary>Add an HX-Redirect header (HTMX) or use NavigateTo (non-HTMX).</summary>
    protected IActionResult HxRedirect(string url)
    {
        if (IsHtmx)
        {
            Response.Headers["HX-Redirect"] = url;
            return new EmptyResult();
        }
        return Redirect(url);
    }

    /// <summary>
    /// Send a MediatR request and convert failures/exceptions to a toast.
    /// Returns null if it failed (caller continues), otherwise the value.
    /// </summary>
    protected async Task<T?> SendAsync<T>(IRequest<Result<T>> request, CancellationToken ct = default)
        where T : class
    {
        try
        {
            var result = await Mediator.Send(request, ct);
            if (result.IsSuccess) return result.Value;
            Toast(result.Error!.Message, ToastLevel.Error);
            return null;
        }
        catch (Exception ex)
        {
            Toast($"Erro: {ex.Message}", ToastLevel.Error);
            return null;
        }
    }

    /// <summary>Same as SendAsync but for value-type results.</summary>
    protected async Task<(bool ok, T value)> SendStructAsync<T>(IRequest<Result<T>> request, CancellationToken ct = default)
        where T : struct
    {
        try
        {
            var result = await Mediator.Send(request, ct);
            if (result.IsSuccess) return (true, result.Value);
            Toast(result.Error!.Message, ToastLevel.Error);
            return (false, default);
        }
        catch (Exception ex)
        {
            Toast($"Erro: {ex.Message}", ToastLevel.Error);
            return (false, default);
        }
    }
}

public enum ToastLevel { Info, Success, Warning, Error }
