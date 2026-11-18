using Lodge.Contracts.Messaging;
using Lodge.Contracts.Notifications;

namespace Lodge.Application.Abstractions.Realtime;

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
