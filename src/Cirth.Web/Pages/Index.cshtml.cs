using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cirth.Web.Pages;

[Authorize]
public sealed class IndexModel : PageModel
{
    public void OnGet() { }
}
