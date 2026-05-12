using Cirth.Application.Features.Documents.UploadDocument;
using Cirth.Application.Features.Tags.AddTagToDocument;
using Cirth.Application.Features.Tags.CreateTag;
using Cirth.Application.Features.Tags.ListTags;
using Cirth.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Cirth.Web.Pages.Documents;

[Authorize]
public sealed class UploadModel(IMediator mediator) : CirthPageModel(mediator)
{
    [BindProperty]
    public UploadInput Input { get; set; } = new();

    public TagPickerVm TagPickerVm { get; private set; } = new([], []);

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadTagsAsync(ct);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        await LoadTagsAsync(ct);

        // Re-validate selection set (HTMX may have left orphans)
        if (Input.SelectedTagIds is null) Input.SelectedTagIds = [];
        TagPickerVm = TagPickerVm with { SelectedIds = [.. Input.SelectedTagIds] };

        if (!ModelState.IsValid)
            return Page();

        if (Input.Mode == "url")
        {
            if (string.IsNullOrWhiteSpace(Input.Url) || !Uri.IsWellFormedUriString(Input.Url, UriKind.Absolute))
            {
                ModelState.AddModelError("Input.Url", "URL inválida.");
                return Page();
            }
        }
        else
        {
            if (Input.File is null || Input.File.Length == 0)
            {
                ModelState.AddModelError("Input.File", "Selecione um arquivo.");
                return Page();
            }
            if (Input.File.Length > 50L * 1024 * 1024)
            {
                ModelState.AddModelError("Input.File", "Arquivo excede 50 MB.");
                return Page();
            }
        }

        UploadDocumentCommand cmd;
        Stream? stream = null;
        try
        {
            if (Input.Mode == "url")
            {
                cmd = new UploadDocumentCommand(
                    Input.Title, "url.txt", Stream.Null, "text/plain", 0,
                    Input.Author.NullIfEmpty(), true, Input.Url);
            }
            else
            {
                stream = Input.File!.OpenReadStream();
                var mime = ResolveMimeType(Input.File);
                cmd = new UploadDocumentCommand(
                    Input.Title, Input.File.FileName, stream, mime, Input.File.Length,
                    Input.Author.NullIfEmpty(), false, null);
            }

            var result = await Mediator.Send(cmd, ct);
            if (!result.IsSuccess)
            {
                Toast(result.Error!.Message, ToastLevel.Error);
                return Page();
            }

            var docId = result.Value!.DocumentId;
            foreach (var tagId in Input.SelectedTagIds)
            {
                try { await Mediator.Send(new AddTagToDocumentCommand(docId, tagId), ct); }
                catch { /* tagging shouldn't block upload success */ }
            }

            Toast("Documento enviado e em processamento!", ToastLevel.Success);
            return RedirectToPage("Index");
        }
        catch (Exception ex)
        {
            Toast($"Erro ao enviar: {ex.Message}", ToastLevel.Error);
            return Page();
        }
        finally
        {
            stream?.Dispose();
        }
    }

    public async Task<IActionResult> OnPostToggleTagAsync(Guid tagId, CancellationToken ct)
    {
        await LoadTagsAsync(ct);
        var selected = (Input.SelectedTagIds ?? []).ToHashSet();
        if (!selected.Remove(tagId)) selected.Add(tagId);
        TagPickerVm = TagPickerVm with { SelectedIds = selected };
        return Partial("_TagPicker", TagPickerVm);
    }

    public async Task<IActionResult> OnPostCreateTagAsync(string? newTagName, string? newTagColor, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(newTagName))
        {
            var color = !string.IsNullOrWhiteSpace(newTagColor) && Regex.IsMatch(newTagColor, "^#[0-9A-Fa-f]{6}$") ? newTagColor : null;
            var result = await Mediator.Send(new CreateTagCommand(newTagName.Trim(), color), ct);
            if (result.IsSuccess)
            {
                Toast($"Tag \"{result.Value!.Name}\" criada.", ToastLevel.Success);
                Input.SelectedTagIds = [.. (Input.SelectedTagIds ?? []), result.Value!.Id];
            }
            else
            {
                Toast(result.Error!.Message, ToastLevel.Warning);
            }
        }

        await LoadTagsAsync(ct);
        TagPickerVm = TagPickerVm with { SelectedIds = [.. (Input.SelectedTagIds ?? [])] };
        return Partial("_TagPicker", TagPickerVm);
    }

    private async Task LoadTagsAsync(CancellationToken ct)
    {
        try
        {
            var tags = await Mediator.Send(new ListTagsQuery(), ct);
            if (tags.IsSuccess)
            {
                TagPickerVm = new TagPickerVm(tags.Value!, [.. (Input.SelectedTagIds ?? [])]);
            }
        }
        catch { /* tags will be empty — user can still create inline */ }
    }

    private static string ResolveMimeType(IFormFile file)
    {
        var ct = file.ContentType;
        if (!string.IsNullOrWhiteSpace(ct) && ct != "application/octet-stream")
            return ct;
        return Path.GetExtension(file.FileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".md" or ".markdown" => "text/markdown",
            ".html" or ".htm" => "text/html",
            _ => "text/plain"
        };
    }

    public sealed class UploadInput
    {
        [Required(ErrorMessage = "Título é obrigatório.")]
        [StringLength(500)]
        public string Title { get; set; } = string.Empty;

        public string Mode { get; set; } = "file";
        public IFormFile? File { get; set; }
        public string? Url { get; set; }
        public string? Author { get; set; }
        public List<Guid> SelectedTagIds { get; set; } = [];
    }
}

public sealed record TagPickerVm(
    IReadOnlyList<Cirth.Application.Features.Tags.CreateTag.TagDto> Tags,
    HashSet<Guid> SelectedIds);
