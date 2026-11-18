using Lodge.Application.Abstractions.Realtime;
using Lodge.Contracts.Messaging;
using Lodge.Contracts.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace Lodge.Presentation.Realtime;

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
