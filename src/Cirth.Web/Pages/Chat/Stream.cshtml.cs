using Cirth.Application.Features.Chat.SendMessage;
using Cirth.Shared;
using Cirth.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using System.Text;

namespace Cirth.Web.Pages.Chat;

[Authorize]
public sealed class StreamModel(IMediator mediator, IMemoryCache cache) : PageModel
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
        Response.Headers["X-Accel-Buffering"] = "no";

        async Task WriteEvent(string evt, string data)
        {
            var msg = $"event: {evt}\ndata: {data.Replace("\n", "\ndata: ")}\n\n";
            await Response.WriteAsync(msg, ct);
            await Response.Body.FlushAsync(ct);
        }

        Result<IAsyncEnumerable<string>> result;
        try
        {
            result = await mediator.Send(new SendMessageCommand(pending.ConversationId, pending.Content), ct);
        }
        catch (Exception ex)
        {
            await WriteEvent("token", $"<em>Erro: {System.Net.WebUtility.HtmlEncode(ex.Message)}</em>");
            await WriteEvent("done", "end");
            return;
        }

        if (!result.IsSuccess)
        {
            await WriteEvent("token", $"<em>Erro: {System.Net.WebUtility.HtmlEncode(result.Error!.Message)}</em>");
            await WriteEvent("done", "end");
            return;
        }

        var buffer = new StringBuilder();
        try
        {
            await foreach (var token in result.Value!.WithCancellation(ct))
            {
                buffer.Append(token);
                // Emit only the new token (HTML-encoded). Client appends because hx-swap="beforeend".
                await WriteEvent("token", System.Net.WebUtility.HtmlEncode(token));
            }
        }
        catch (Exception ex)
        {
            await WriteEvent("token", $"<em>Erro de streaming: {System.Net.WebUtility.HtmlEncode(ex.Message)}</em>");
        }

        await WriteEvent("done", "end");
    }
}
