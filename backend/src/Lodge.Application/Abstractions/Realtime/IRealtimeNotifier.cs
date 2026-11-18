using Lodge.Contracts.Messaging;
using Lodge.Contracts.Notifications;

namespace Lodge.Application.Abstractions.Realtime;

/// <summary>
/// Pushes events to the signed-in users' browsers (SignalR hub <c>/hubs/notifications</c>). Every method targets
/// users, never connections: a user with several tabs receives the event in each of them.
/// </summary>
public interface IRealtimeNotifier
{
    /// <summary>
    /// <c>messageReceived { conversationId, message }</c>.
    /// </summary>
    Task MessageReceivedAsync(
        IReadOnlyCollection<Guid> userIds,
        Guid conversationId,
        MessageResponse message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>conversationUpdated { conversationId }</c> (new message, read marker moved, …).
    /// </summary>
    Task ConversationUpdatedAsync(
        IReadOnlyCollection<Guid> userIds,
        Guid conversationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>notificationReceived { notification }</c>.
    /// </summary>
    Task NotificationReceivedAsync(
        Guid userId,
        NotificationResponse notification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>unreadCounts { messages, notifications }</c>.
    /// </summary>
    Task UnreadCountsAsync(Guid userId, UnreadCountsResponse counts, CancellationToken cancellationToken = default);
}

/// <summary>
/// Used when no realtime transport is registered (e.g. hosts without the presentation layer).
/// </summary>
internal sealed class NullRealtimeNotifier : IRealtimeNotifier
{
    public Task MessageReceivedAsync(
        IReadOnlyCollection<Guid> userIds,
        Guid conversationId,
        MessageResponse message,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task ConversationUpdatedAsync(
        IReadOnlyCollection<Guid> userIds,
        Guid conversationId,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task NotificationReceivedAsync(
        Guid userId,
        NotificationResponse notification,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task UnreadCountsAsync(Guid userId, UnreadCountsResponse counts, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
