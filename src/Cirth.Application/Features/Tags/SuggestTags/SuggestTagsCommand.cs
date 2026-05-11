using Cirth.Application.Common.Ports;
using Cirth.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cirth.Application.Features.Tags.SuggestTags;

public sealed record SuggestTagsCommand(Guid DocumentId) : IRequest<Result<IReadOnlyList<string>>>;

internal sealed class SuggestTagsCommandHandler(
    ITenantProvider tenantProvider,
    ILlmChatService llmService,
    IQueryDbContext db) : IRequestHandler<SuggestTagsCommand, Result<IReadOnlyList<string>>>
{
    public async Task<Result<IReadOnlyList<string>>> Handle(SuggestTagsCommand cmd, CancellationToken ct)
    {
        var tenantId = tenantProvider.CurrentTenantId.Value;

        var doc = await db.Documents
            .Where(d => d.Id.Value == cmd.DocumentId && d.TenantId.Value == tenantId)
            .FirstOrDefaultAsync(ct);
        if (doc is null)
            return Error.NotFound("document.not_found", "Document not found.");

        // Get first few chunks as sample for tag suggestion
        var sample = await db.Chunks
            .Where(c => c.TenantId.Value == tenantId && c.DocumentVersionId.Value == doc.CurrentVersionId!.Value.Value && c.IsCurrent)
            .OrderBy(c => c.Ordinal)
            .Take(3)
            .Select(c => c.Content)
            .ToListAsync(ct);

        if (sample.Count == 0)
            return Result<IReadOnlyList<string>>.Success([]);

        var sampleText = string.Join("\n\n", sample);
        var systemPrompt = "You suggest 3-5 concise tags for documents. Return only a comma-separated list of tags, lowercase, no punctuation.";
        var userPrompt = $"Document title: {doc.Title}\n\nContent sample:\n{sampleText[..Math.Min(2000, sampleText.Length)]}";

        var response = await llmService.CompleteChatAsync(systemPrompt, userPrompt, "gpt-4.1-mini", ct);
        var tags = response.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(5).ToList();

        return Result<IReadOnlyList<string>>.Success(tags);
    }
}
