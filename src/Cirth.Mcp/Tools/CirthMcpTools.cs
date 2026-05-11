using Cirth.Application.Features.Documents.GetDocument;
using Cirth.Application.Features.SavedAnswers.ListSavedAnswers;
using Cirth.Application.Features.Search.HybridSearch;
using Cirth.Application.Features.Tags.ListTags;
using Cirth.Application.Features.Collections.ListCollections;
using Cirth.Application.Features.Chat.CreateConversation;
using Cirth.Application.Features.Chat.SendMessage;
using Cirth.Shared;
using MediatR;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Cirth.Mcp.Tools;

[McpServerToolType]
public sealed class CirthMcpTools(IMediator mediator)
{
    [McpServerTool, Description("Search documents in the knowledge base using hybrid BM25 + semantic search.")]
    public async Task<string> search_documents(
        [Description("The search query")] string query,
        [Description("Filter by tag name (optional)")] string? tag = null,
        [Description("Maximum number of results (default 8)")] int limit = 8,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new HybridSearchQuery(query, limit), ct);
        if (result.IsFailure)
            return $"Error: {result.Error!.Message}";

        if (result.Value!.Hits.Count == 0)
            return "No results found.";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Found {result.Value.Hits.Count} result(s) for \"{query}\":\n");
        foreach (var (hit, i) in result.Value.Hits.Select((h, i) => (h, i)))
        {
            sb.AppendLine($"[{i + 1}] **{hit.DocumentTitle}** (score: {hit.Score:F3})");
            sb.AppendLine(hit.Content[..Math.Min(300, hit.Content.Length)]);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    [McpServerTool, Description("Ask a question and get an AI-generated answer based on the knowledge base.")]
    public async Task<string> ask_question(
        [Description("The question to answer")] string question,
        [Description("Existing conversation ID to continue (optional)")] string? conversation_id = null,
        CancellationToken ct = default)
    {
        Guid convId;

        if (conversation_id is not null && Guid.TryParse(conversation_id, out var existing))
        {
            convId = existing;
        }
        else
        {
            var conv = await mediator.Send(new CreateConversationCommand("MCP Session"), ct);
            if (conv.IsFailure) return $"Error creating conversation: {conv.Error!.Message}";
            convId = conv.Value!.Id;
        }

        var result = await mediator.Send(new SendMessageCommand(convId, question), ct);
        if (result.IsFailure)
            return $"Error: {result.Error!.Message}";

        var sb = new System.Text.StringBuilder();
        await foreach (var token in result.Value!)
            sb.Append(token);

        return sb.ToString();
    }

    [McpServerTool, Description("Get full metadata and content of a document by its ID.")]
    public async Task<string> get_document(
        [Description("The document ID (UUID)")] string document_id,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(document_id, out var id))
            return "Invalid document ID format.";

        var result = await mediator.Send(new GetDocumentQuery(id), ct);
        if (result.IsFailure)
            return $"Error: {result.Error!.Message}";

        var doc = result.Value!;
        return $"""
            # {doc.Title}
            - Type: {doc.SourceType}
            - Status: {doc.Status}
            - Author: {doc.Author ?? "Unknown"}
            - Versions: {doc.Versions.Count}
            - Tags: {string.Join(", ", doc.Tags.Select(t => t.Name))}
            - Collections: {string.Join(", ", doc.Collections.Select(c => c.Name))}
            - Created: {doc.CreatedAt:yyyy-MM-dd}
            """;
    }

    [McpServerTool, Description("List all tags in the knowledge base.")]
    public async Task<string> list_tags(CancellationToken ct = default)
    {
        var result = await mediator.Send(new ListTagsQuery(), ct);
        if (result.IsFailure)
            return $"Error: {result.Error!.Message}";

        if (result.Value!.Count == 0)
            return "No tags found.";

        return string.Join("\n", result.Value!.Select(t => $"- {t.Name}"));
    }

    [McpServerTool, Description("List all collections in the knowledge base.")]
    public async Task<string> list_collections(CancellationToken ct = default)
    {
        var result = await mediator.Send(new ListCollectionsQuery(), ct);
        if (result.IsFailure)
            return $"Error: {result.Error!.Message}";

        if (result.Value!.Count == 0)
            return "No collections found.";

        return string.Join("\n", result.Value!.Select(c => $"- {c.Name} ({c.DocumentCount} docs)"));
    }

    [McpServerTool, Description("List saved answers, optionally filtered by a search query.")]
    public async Task<string> list_saved_answers(
        [Description("Optional search query to filter saved answers")] string? query = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new ListSavedAnswersQuery(query), ct);
        if (result.IsFailure)
            return $"Error: {result.Error!.Message}";

        if (result.Value!.Count == 0)
            return "No saved answers found.";

        var sb = new System.Text.StringBuilder();
        foreach (var a in result.Value!.Take(10))
        {
            sb.AppendLine($"**Q: {a.Question}**");
            sb.AppendLine(a.Answer[..Math.Min(200, a.Answer.Length)]);
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
