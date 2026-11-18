using Lodge.Application.Notifications;
using Lodge.Contracts.Notifications;
using Lodge.Domain.Core.Primitives;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Lodge.Presentation.Realtime;

/// <summary>
/// The realtime channel of signed-in users, mapped at <see cref="Path"/> (outside <c>/api/v1</c>). Authenticate with
/// the access token: the <c>Authorization</c> header, or the <c>access_token</c> query string (WebSockets).
/// </summary>
/// <remarks>
/// Server → client events (camelCase JSON): <c>messageReceived { conversationId, message }</c>,
/// <c>conversationUpdated { conversationId }</c>, <c>notificationReceived { notification }</c> and
/// <c>unreadCounts { messages, notifications }</c> (also sent right after connecting). Clients call no hub methods.
/// </remarks>
[Authorize]
public sealed class NotificationsHub(ISender sender) : Hub
{
    public const string Path = "/hubs/notifications";

    public const string MessageReceived = "messageReceived";

    public const string ConversationUpdated = "conversationUpdated";

    public const string NotificationReceived = "notificationReceived";

    public const string UnreadCounts = "unreadCounts";

    public override async Task OnConnectedAsync()
    {
        if (Guid.TryParse(Context.UserIdentifier, out Guid userId))
        {
            Result<UnreadCountsResponse> counts = await sender.Send(new GetUnreadCountsQuery(userId), Context.ConnectionAborted);

            if (counts.IsSuccess)
            {
                await Clients.Caller.SendAsync(UnreadCounts, counts.Value, Context.ConnectionAborted);
            }
        }

        await base.OnConnectedAsync();
    }
}
