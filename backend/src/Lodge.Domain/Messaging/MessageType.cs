namespace Lodge.Domain.Messaging;

/// <summary>
/// The kind of a message (stored and serialized by name).
/// </summary>
public enum MessageType
{
    /// <summary>
    /// Written by the guest or the host.
    /// </summary>
    User = 1,

    /// <summary>
    /// Posted by Lodge on a booking event (see <see cref="SystemEvents"/>); it has no sender.
    /// </summary>
    System = 2,
}
