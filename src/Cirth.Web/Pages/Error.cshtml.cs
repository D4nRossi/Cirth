using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace Cirth.Web.Pages;

[AllowAnonymous]
public sealed class ErrorModel : PageModel
{
    public int StatusCodeValue { get; private set; } = 500;
    public string? RequestId { get; private set; }

    public void OnGet(int? code)
    {
        StatusCodeValue = code ?? 500;
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        Response.StatusCode = StatusCodeValue;
    }
}
