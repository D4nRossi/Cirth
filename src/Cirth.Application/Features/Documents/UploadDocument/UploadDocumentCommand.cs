using Cirth.Application.Common.Ports;
using Cirth.Domain.Documents;
using Cirth.Domain.Jobs;
using Cirth.Shared;
using FluentValidation;
using MediatR;
using System.Text.Json;

namespace Cirth.Application.Features.Documents.UploadDocument;

public sealed record UploadDocumentCommand(
    string Title,
    string FileName,
    Stream Content,
    string MimeType,
    long SizeBytes,
    string? Author,
    bool IsUrl,
    string? Url) : IRequest<Result<UploadDocumentResult>>;

public sealed record UploadDocumentResult(Guid DocumentId, Guid VersionId);

public sealed class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
{
    private static readonly string[] AllowedMimes =
    [
        "application/pdf", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "text/markdown", "text/plain", "text/html", "text/x-markdown"
    ];

    public UploadDocumentCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.SizeBytes).LessThanOrEqualTo(50L * 1024 * 1024).WithMessage("File exceeds 50 MB limit.");
        When(x => !x.IsUrl, () =>
            RuleFor(x => x.MimeType).Must(m => AllowedMimes.Contains(m))
                .WithMessage("Unsupported file type."));
        When(x => x.IsUrl, () =>
            RuleFor(x => x.Url).NotEmpty().Must(u => Uri.IsWellFormedUriString(u, UriKind.Absolute)));
    }
}

internal sealed class UploadDocumentCommandHandler(
    ITenantProvider tenantProvider,
    IObjectStorage objectStorage,
    IJobQueue jobQueue,
    IDocumentRepository documentRepository,
    IUnitOfWork uow) : IRequestHandler<UploadDocumentCommand, Result<UploadDocumentResult>>
{
    public async Task<Result<UploadDocumentResult>> Handle(UploadDocumentCommand cmd, CancellationToken ct)
    {
        var tenantId = tenantProvider.CurrentTenantId;
        var userId = tenantProvider.CurrentUserId;

        var sourceType = DetermineSourceType(cmd);
        var document = Document.Create(tenantId, cmd.Title, sourceType, cmd.Author);

        string storageKey;
        string contentHash;

        if (cmd.IsUrl)
        {
            storageKey = $"urls/{tenantId.Value}/{document.Id.Value}/ref.txt";
            contentHash = ComputeHash(cmd.Url!);
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cmd.Url!));
            await objectStorage.PutAsync("cirth-uploads", storageKey, stream, "text/plain", ct);
        }
        else
        {
            storageKey = $"{tenantId.Value}/{document.Id.Value}/v1/{cmd.FileName}";
            // BrowserFileStream from Blazor is not seekable; buffer so we can hash then rewind.
            using var buf = new MemoryStream();
            await cmd.Content.CopyToAsync(buf, ct);
            buf.Position = 0;
            contentHash = await ComputeHashAsync(buf, ct);
            buf.Position = 0;
            await objectStorage.PutAsync("cirth-uploads", storageKey, buf, cmd.MimeType, ct);
        }

        var version = document.AddVersion(contentHash, storageKey, cmd.SizeBytes, cmd.MimeType);
        await documentRepository.AddAsync(document, ct);

        var payload = JsonSerializer.Serialize(new ProcessDocumentPayload(
            document.Id.Value, version.Id.Value, tenantId.Value, userId.Value,
            storageKey, cmd.MimeType, cmd.IsUrl, cmd.Url));

        await jobQueue.EnqueueAsync("ProcessDocument", payload, ct);
        await uow.CommitAsync(ct);

        return Result<UploadDocumentResult>.Success(new(document.Id.Value, version.Id.Value));
    }

    private static DocumentSourceType DetermineSourceType(UploadDocumentCommand cmd)
    {
        if (cmd.IsUrl) return DocumentSourceType.WebLink;
        return cmd.MimeType switch
        {
            "application/pdf" => DocumentSourceType.Pdf,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => DocumentSourceType.Docx,
            "text/markdown" or "text/x-markdown" => DocumentSourceType.Markdown,
            "text/html" => DocumentSourceType.Html,
            _ => DocumentSourceType.Text
        };
    }

    private static string ComputeHash(string text)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static async Task<string> ComputeHashAsync(Stream stream, CancellationToken ct)
    {
        var bytes = await System.Security.Cryptography.SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

public sealed record ProcessDocumentPayload(
    Guid DocumentId, Guid VersionId, Guid TenantId, Guid UserId,
    string StorageKey, string MimeType, bool IsUrl, string? Url);
