using Cirth.Application.Features.Chat.CreateConversation;
using Cirth.Application.Features.Chat.GetConversation;
using Cirth.Application.Features.Chat.ListConversations;
using Cirth.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Cirth.Web.Pages.Chat;

[Authorize]
public sealed class IndexModel(IMediator mediator, IMemoryCache cache) : CirthPageModel(mediator)
{
    public Guid? Id { get; private set; }
    public IReadOnlyList<ConversationSummaryDto> Conversations { get; private set; } = [];
    public ConversationDetailDto? Conversation { get; private set; }

    public async Task OnGetAsync(Guid? id, CancellationToken ct)
    {
        Id = id == Guid.Empty ? null : id;

        var convs = await Mediator.Send(new ListConversationsQuery(), ct);
        if (convs.IsSuccess) Conversations = convs.Value!;

        if (Id is { } guidId)
        {
            var conv = await Mediator.Send(new GetConversationQuery(guidId), ct);
            if (conv.IsSuccess) Conversation = conv.Value;
        }
    }

    public async Task<IActionResult> OnPostNewConversationAsync(CancellationToken ct)
    {
        var result = await Mediator.Send(new CreateConversationCommand(), ct);
        if (!result.IsSuccess)
        {
            Toast(result.Error!.Message, ToastLevel.Error);
            return RedirectToPage("Index");
        }
        return HxRedirect($"/Chat/{result.Value!.Id}");
    }

    /// <summary>
    /// POST /Chat/{id}?handler=Send — stashes the request and returns HTML containing
    /// the user bubble + an assistant bubble that connects to /Chat/Stream/{streamId}.
    /// </summary>
    public IActionResult OnPostSend(Guid id, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return new EmptyResult();

        var streamId = Guid.NewGuid().ToString("N");
        cache.Set(StreamCacheKey(streamId), new PendingChatStream(id, content.Trim()), TimeSpan.FromMinutes(2));

        var userBubble = $"<div class=\"bubble user\">{System.Net.WebUtility.HtmlEncode(content)}</div>";
        var assistantBubble = $"""
            <div class="bubble assistant streaming" id="asst-{streamId}"
                 hx-ext="sse" sse-connect="/Chat/Stream/{streamId}"
                 sse-swap="token" hx-swap="beforeend"
                 sse-close="done"></div>
            """;
        return Content(userBubble + assistantBubble, "text/html");
    }

    internal static string StreamCacheKey(string streamId) => $"chat-stream:{streamId}";
}

internal sealed record PendingChatStream(Guid ConversationId, string Content);
