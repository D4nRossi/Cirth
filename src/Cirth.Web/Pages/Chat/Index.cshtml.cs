using Cirth.Application.Features.Chat.CreateConversation;
using Cirth.Application.Features.Chat.GetConversation;
using Cirth.Application.Features.Chat.ListConversations;
using Cirth.Application.Features.SavedAnswers.CreateSavedAnswer;
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
    ///
    /// Bubble shape: two SSE swap targets in one element:
    ///   - .progress (innerHTML swap on event=progress) — pre-stage status messages
    ///   - .content  (beforeend swap on event=token)    — actual LLM tokens
    /// When the server emits event=done, the SSE connection closes and JS strips the
    /// "streaming" class + clears the progress placeholder.
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
                 hx-ext="sse" sse-connect="/Chat/Stream/{streamId}" sse-close="done">
              <div class="progress" sse-swap="progress" hx-swap="innerHTML"><em>⏳ Aguardando...</em></div>
              <div class="content" sse-swap="token" hx-swap="beforeend"></div>
              <div class="actions" sse-swap="actions" hx-swap="innerHTML"></div>
            </div>
            """;
        return Content(userBubble + assistantBubble, "text/html");
    }

    /// <summary>
    /// POST /Chat/{id}?handler=SaveLastAnswer — saves the most recent assistant message
    /// in this conversation (along with its preceding user question) as a SavedAnswer.
    /// The save button is rendered after the SSE stream completes — by then the message
    /// is persisted, so "last assistant message in this conversation" is unambiguous.
    /// </summary>
    public async Task<IActionResult> OnPostSaveLastAnswerAsync(Guid id, CancellationToken ct)
    {
        var conv = await Mediator.Send(new GetConversationQuery(id), ct);
        if (!conv.IsSuccess || conv.Value is null)
        {
            Toast("Conversa não encontrada.", ToastLevel.Error);
            return new EmptyResult();
        }

        var messages = conv.Value.Messages;
        // Messages are ordered ascending by CreatedAt — walk backwards to find the most
        // recent assistant message and the user message immediately preceding it.
        MessageDto? assistant = null;
        MessageDto? userPrev = null;
        for (var i = messages.Count - 1; i >= 0; i--)
        {
            if (assistant is null && string.Equals(messages[i].Role, "Assistant", StringComparison.OrdinalIgnoreCase))
            {
                assistant = messages[i];
            }
            else if (assistant is not null && string.Equals(messages[i].Role, "User", StringComparison.OrdinalIgnoreCase))
            {
                userPrev = messages[i];
                break;
            }
        }

        if (assistant is null || userPrev is null)
        {
            Toast("Nenhuma resposta para salvar ainda.", ToastLevel.Warning);
            return new EmptyResult();
        }

        var save = await Mediator.Send(new CreateSavedAnswerCommand(
            id,
            assistant.Id,
            userPrev.Content,
            assistant.Content,
            assistant.CitedChunkIds ?? []), ct);

        if (save.IsSuccess)
            Toast("Resposta salva — acesse em /Saved.", ToastLevel.Success);
        else
            Toast(save.Error!.Message, ToastLevel.Error);

        return new EmptyResult();
    }

    internal static string StreamCacheKey(string streamId) => $"chat-stream:{streamId}";
}

internal sealed record PendingChatStream(Guid ConversationId, string Content);
