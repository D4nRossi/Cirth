using Cirth.Application.Features.Chat.SendMessage;
using Cirth.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;

namespace Cirth.Web.Pages.Chat;

[Authorize]
public sealed class StreamModel(
    IMediator mediator,
    IMemoryCache cache,
    ILogger<StreamModel> logger) : PageModel
{
    public async Task OnGetAsync(string streamId, CancellationToken ct)
    {
        var key = IndexModel.StreamCacheKey(streamId);
        if (!cache.TryGetValue<PendingChatStream>(key, out var pending) || pending is null)
        {
            Response.StatusCode = 404;
            return;
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

        logger.LogInformation("Chat stream {StreamId} starting for conv {Conv}", streamId, pending.ConversationId);

        await WriteEvent("progress", "<em>🔍 Analisando sua pergunta...</em>");

        Result<IAsyncEnumerable<string>> result;
        try
        {
            // The SendMessageCommand bundles: saved-answer check + hybrid search + LLM call.
            // We can't easily emit progress between those sub-stages without refactoring the
            // command, so we fire two cosmetic progress messages around it.
            result = await mediator.Send(new SendMessageCommand(pending.ConversationId, pending.Content), ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Chat stream {StreamId} failed to dispatch", streamId);
            await WriteEvent("progress", $"<span class=\"err\">Erro: {System.Net.WebUtility.HtmlEncode(ex.Message)}</span>");
            await WriteEvent("done", "end");
            return;
        }

        if (!result.IsSuccess)
        {
            logger.LogWarning("Chat stream {StreamId} returned Result.Failure: {Error}", streamId, result.Error!.Message);
            await WriteEvent("progress", $"<span class=\"err\">Erro: {System.Net.WebUtility.HtmlEncode(result.Error!.Message)}</span>");
            await WriteEvent("done", "end");
            return;
        }

        await WriteEvent("progress", "<em>📚 Buscando contexto no acervo...</em>");
        // Tiny pacing pause so the UI shows the progress for at least a frame.
        // Doesn't affect total time noticeably and ensures both progress events render.
        await Task.Delay(120, ct);
        await WriteEvent("progress", "<em>🤖 Gerando resposta...</em>");

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

        // Clear the progress placeholder when generation finishes.
        await WriteEvent("progress", "");
        await WriteEvent("done", "end");

        logger.LogInformation("Chat stream {StreamId} completed", streamId);
    }
}
