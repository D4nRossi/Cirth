using Cirth.Application.Features.Tags.CreateTag;
using Cirth.Application.Features.Tags.ListTags;
using Cirth.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Cirth.Web.Pages.Tags;

[Authorize]
public sealed class IndexModel(IMediator mediator) : CirthPageModel(mediator)
{
    public IReadOnlyList<TagDto> Tags { get; private set; } = [];

    [BindProperty]
    public NewTagInput NewTag { get; set; } = new();

    public async Task OnGetAsync(CancellationToken ct)
    {
        Tags = await Mediator.Send(new ListTagsQuery(), ct) is { IsSuccess: true, Value: { } v } ? v : [];
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(NewTag.Name))
        {
            ModelState.AddModelError(nameof(NewTag) + "." + nameof(NewTag.Name), "Nome é obrigatório.");
        }

        var color = IsValidHex(NewTag.Color) ? NewTag.Color : null;
        if (ModelState.IsValid)
        {
            var result = await Mediator.Send(new CreateTagCommand(NewTag.Name.Trim(), color), ct);
            if (result.IsSuccess)
            {
                Toast($"Tag \"{result.Value!.Name}\" criada.", ToastLevel.Success);
                NewTag = new();
                ModelState.Clear();
            }
            else
            {
                Toast(result.Error!.Message, ToastLevel.Error);
            }
        }

        Tags = await Mediator.Send(new ListTagsQuery(), ct) is { IsSuccess: true, Value: { } v } ? v : [];
        return Partial("_TagList", Tags);
    }

    private static bool IsValidHex(string? c) =>
        !string.IsNullOrWhiteSpace(c) && Regex.IsMatch(c, "^#[0-9A-Fa-f]{6}$");

    public sealed class NewTagInput
    {
        [Required(ErrorMessage = "Nome é obrigatório.")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(7)]
        public string? Color { get; set; }
    }
}
