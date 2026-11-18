using System.Security.Claims;
using Lodge.Application.Abstractions.Realtime;
using Lodge.Application.Notifications;
using Lodge.Contracts.Messaging;
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

/// <summary>
/// Identifies SignalR users by the access token's <c>userId</c> claim, so that events can target users.
/// </summary>
internal sealed class UserIdClaimUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) => connection.User?.FindFirst("userId")?.Value;
}

/// <summary>
/// Pushes realtime events through the hub (and the Redis backplane, so every API instance reaches its connections).
/// </summary>
internal sealed class SignalRRealtimeNotifier(IHubContext<NotificationsHub> hubContext) : IRealtimeNotifier
{
    public Task MessageReceivedAsync(
        IReadOnlyCollection<Guid> userIds,
        Guid conversationId,
        MessageResponse message,
        CancellationToken cancellationToken = default) =>
        hubContext.Clients.Users(Ids(userIds)).SendAsync(
            NotificationsHub.MessageReceived,
            new MessageReceivedEvent(conversationId, message),
            cancellationToken);

    public Task ConversationUpdatedAsync(
        IReadOnlyCollection<Guid> userIds,
        Guid conversationId,
        CancellationToken cancellationToken = default) =>
        hubContext.Clients.Users(Ids(userIds)).SendAsync(
            NotificationsHub.ConversationUpdated,
            new ConversationUpdatedEvent(conversationId),
            cancellationToken);

    public Task NotificationReceivedAsync(
        Guid userId,
        NotificationResponse notification,
        CancellationToken cancellationToken = default) =>
        hubContext.Clients.User(userId.ToString()).SendAsync(
            NotificationsHub.NotificationReceived,
            new NotificationReceivedEvent(notification),
            cancellationToken);

    public Task UnreadCountsAsync(Guid userId, UnreadCountsResponse counts, CancellationToken cancellationToken = default) =>
        hubContext.Clients.User(userId.ToString()).SendAsync(NotificationsHub.UnreadCounts, counts, cancellationToken);

    private static List<string> Ids(IEnumerable<Guid> userIds) => [.. userIds.Distinct().Select(id => id.ToString())];
}

/// <summary>
/// The payload of <c>messageReceived</c>.
/// </summary>
public sealed record MessageReceivedEvent(Guid ConversationId, MessageResponse Message);

/// <summary>
/// The payload of <c>conversationUpdated</c>.
/// </summary>
public sealed record ConversationUpdatedEvent(Guid ConversationId);

/// <summary>
/// The payload of <c>notificationReceived</c>.
/// </summary>
public sealed record NotificationReceivedEvent(NotificationResponse Notification);
