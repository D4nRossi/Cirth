using Cirth.Application.Features.Chat.SendMessage;
using Cirth.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;

namespace Cirth.Web.Pages.Chat;

[Authorize]
public sealed class StreamModel(
    IMediator mediator,
    IMemoryCache cache,
    ILogger<StreamModel> logger) : PageModel
{
    // Returns IActionResult so we can end with `new EmptyResult()`. Without it Razor Pages
    // applies an implicit PageResult AFTER our SSE handler completes, which tries to set
    // Content-Type=text/html on an already-started response and explodes with
    // "Headers are read-only, response has already started".
    public async Task<IActionResult> OnGetAsync(string streamId, CancellationToken ct)
    {
        var key = IndexModel.StreamCacheKey(streamId);
        if (!cache.TryGetValue<PendingChatStream>(key, out var pending) || pending is null)
        {
            return NotFound();
        }
        cache.Remove(key);

        Response.Headers["Content-Type"] = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache, no-store";
        Response.Headers["X-Accel-Buffering"] = "no"; // disable Nginx buffering
        // Disable response buffering at the Kestrel level so SSE events ship as written.
        var bodyFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>();
        bodyFeature?.DisableBuffering();

        async Task WriteEvent(string evt, string data)
        {
            var msg = $"event: {evt}\ndata: {data.Replace("\n", "\ndata: ")}\n\n";
            await Response.WriteAsync(msg, ct);
            await Response.Body.FlushAsync(ct);
        }

        // SSE padding comment: forces the HTTP/2 layer to flush the response headers and a
        // first frame immediately. Without it, Kestrel waits for ~4KB of body before sending
        // anything over HTTP/2, hiding our first progress event for several hundred ms.
        // The `:` prefix makes it a comment in the SSE format — clients ignore it.
        await Response.WriteAsync(":" + new string(' ', 2048) + "\n\n", ct);
        await Response.Body.FlushAsync(ct);

        logger.LogInformation("Chat stream {StreamId} starting for conv {Conv}", streamId, pending.ConversationId);

        await WriteEvent("progress", "<em>🔍 Analisando sua pergunta...</em>");

        Result<IAsyncEnumerable<string>> result;
        try
        {
            // The SendMessageCommand bundles: saved-answer check + hybrid search + LLM call.
            // We can't easily emit progress between those sub-stages without refactoring the
            // command, so we fire cosmetic progress messages around it with deliberate
            // pacing so each one is on screen long enough for the user to read.
            result = await mediator.Send(new SendMessageCommand(pending.ConversationId, pending.Content), ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Chat stream {StreamId} failed to dispatch", streamId);
            await WriteEvent("progress", $"<span class=\"err\">Erro: {System.Net.WebUtility.HtmlEncode(ex.Message)}</span>");
            await WriteEvent("done", "end");
            return new EmptyResult();
        }

        if (!result.IsSuccess)
        {
            logger.LogWarning("Chat stream {StreamId} returned Result.Failure: {Error}", streamId, result.Error!.Message);
            await WriteEvent("progress", $"<span class=\"err\">Erro: {System.Net.WebUtility.HtmlEncode(result.Error!.Message)}</span>");
            await WriteEvent("done", "end");
            return new EmptyResult();
        }

        await WriteEvent("progress", "<em>📚 Buscando contexto no acervo...</em>");
        await Task.Delay(400, ct);
        await WriteEvent("progress", "<em>🤖 Gerando resposta...</em>");
        await Task.Delay(250, ct);

        try
        {
            await foreach (var token in result.Value!.WithCancellation(ct))
            {
                await WriteEvent("token", System.Net.WebUtility.HtmlEncode(token));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Chat stream {StreamId} failed mid-stream", streamId);
            await WriteEvent("token", $"<em>Erro de streaming: {System.Net.WebUtility.HtmlEncode(ex.Message)}</em>");
        }

        // Clear the progress placeholder and append a "Save answer" affordance OOB-style.
        // The save button is appended to the bubble as a sibling of `.content` via a
        // dedicated SSE swap target on the bubble itself (sse-swap="actions").
        await WriteEvent("progress", "");
        var saveBtn = $"""
            <button type="button" class="btn btn-text btn-sm chat-save"
                    hx-post="/Chat/{pending.ConversationId}?handler=SaveLastAnswer"
                    hx-swap="none"
                    title="Salvar resposta para consultar depois">
              ☆ Salvar resposta
            </button>
            """;
        await WriteEvent("actions", saveBtn);
        await WriteEvent("done", "end");

        logger.LogInformation("Chat stream {StreamId} completed", streamId);
        return new EmptyResult();
    }
}
