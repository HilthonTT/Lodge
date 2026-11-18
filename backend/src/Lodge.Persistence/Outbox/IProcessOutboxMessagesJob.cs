namespace Lodge.Persistence.Outbox;

/// <summary>
/// Represents the process outbox messages job interface.
/// </summary>
public interface IProcessOutboxMessagesJob
{
    /// <summary>
    /// Processes the outbox messages.
    /// </summary>
    /// <returns>The completed task.</returns>
    Task ProcessAsync();
}
