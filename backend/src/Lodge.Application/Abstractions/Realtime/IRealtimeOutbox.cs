namespace Lodge.Application.Abstractions.Realtime;

/// <summary>
/// Collects the realtime events of a unit of work and pushes them once the changes are committed, so that clients
/// never hear about data they cannot read yet. Commands are flushed by the unit of work pipeline; background writers
/// (event handlers, jobs) call <see cref="FlushAsync"/> after saving.
/// </summary>
public interface IRealtimeOutbox
{
    /// <summary>
    /// A message was posted: both participants get <c>messageReceived</c>, <c>conversationUpdated</c> and new
    /// unread counts.
    /// </summary>
    void MessagePosted(Guid messageId);

    /// <summary>
    /// A conversation changed (e.g. a read marker moved): its participants get <c>conversationUpdated</c> and new
    /// unread counts.
    /// </summary>
    void ConversationChanged(Guid conversationId);

    /// <summary>
    /// A notification was created: its user gets <c>notificationReceived</c> and new unread counts.
    /// </summary>
    void NotificationCreated(Guid notificationId);

    /// <summary>
    /// The user's unread counters changed.
    /// </summary>
    void CountsChanged(Guid userId);

    /// <summary>
    /// Pushes the collected events. Failures are logged, never thrown: realtime delivery is best effort.
    /// </summary>
    Task FlushAsync(CancellationToken cancellationToken = default);
}
