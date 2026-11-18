using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Messaging;

/// <summary>
/// The message thread between a guest and the host of a listing: one per (listing, guest), reused across bookings.
/// </summary>
/// <remarks>
/// Each participant has a read marker (<see cref="GuestLastReadOnUtc"/>, <see cref="HostLastReadOnUtc"/>): messages
/// sent by the other participant (or by the system) after it are unread.
/// </remarks>
public sealed class Conversation : Entity
{
    private Conversation(Guid id, Guid apartmentId, Guid guestId, Guid hostId, DateTime utcNow)
        : base(id)
    {
        ApartmentId = apartmentId;
        GuestId = guestId;
        HostId = hostId;
        CreatedOnUtc = utcNow;
        LastMessageOnUtc = utcNow;
    }

    /// <remarks>
    /// Required by EF Core.
    /// </remarks>
    private Conversation()
    {
    }

    public Guid ApartmentId { get; private set; }

    public Guid GuestId { get; private set; }

    public Guid HostId { get; private set; }

    public DateTime CreatedOnUtc { get; private set; }

    public DateTime LastMessageOnUtc { get; private set; }

    public DateTime? GuestLastReadOnUtc { get; private set; }

    public DateTime? HostLastReadOnUtc { get; private set; }

    /// <summary>
    /// Gets when the guest was last e-mailed about a new message of this conversation (e-mails are throttled).
    /// </summary>
    public DateTime? GuestLastEmailedOnUtc { get; private set; }

    /// <summary>
    /// Gets when the host was last e-mailed about a new message of this conversation (e-mails are throttled).
    /// </summary>
    public DateTime? HostLastEmailedOnUtc { get; private set; }

    /// <summary>
    /// Starts the conversation between a guest and the host of a listing.
    /// </summary>
    public static Result<Conversation> Start(Guid apartmentId, Guid guestId, Guid hostId, DateTime utcNow)
    {
        if (guestId == hostId)
        {
            return Result.Failure<Conversation>(MessagingErrors.OwnListing);
        }

        return new Conversation(Guid.NewGuid(), apartmentId, guestId, hostId, utcNow);
    }

    public bool IsParticipant(Guid userId) => userId == GuestId || userId == HostId;

    /// <summary>
    /// Gets the other participant of <paramref name="userId"/>.
    /// </summary>
    public Guid OtherParticipant(Guid userId) => userId == GuestId ? HostId : GuestId;

    /// <summary>
    /// Writes a message from a participant.
    /// </summary>
    public Result<Message> Send(Guid senderId, string? body, DateTime utcNow)
    {
        if (!IsParticipant(senderId))
        {
            return Result.Failure<Message>(MessagingErrors.ConversationNotFound(Id));
        }

        Result<Message> message = Message.CreateUser(this, senderId, body, utcNow);

        if (message.IsSuccess)
        {
            Touch(utcNow);

            // Writing a message also means having read the thread.
            MarkRead(senderId, utcNow);
        }

        return message;
    }

    /// <summary>
    /// Posts a system message about a booking event.
    /// </summary>
    public Message Post(string systemEvent, Guid? bookingId, string fallbackBody, DateTime utcNow)
    {
        Message message = Message.CreateSystem(this, systemEvent, bookingId, fallbackBody, utcNow);

        Touch(utcNow);

        return message;
    }

    /// <summary>
    /// Moves the participant's read marker to <paramref name="utcNow"/>.
    /// </summary>
    public void MarkRead(Guid userId, DateTime utcNow)
    {
        if (userId == GuestId)
        {
            GuestLastReadOnUtc = utcNow;
        }
        else if (userId == HostId)
        {
            HostLastReadOnUtc = utcNow;
        }
    }

    public DateTime? LastReadOnUtc(Guid userId) => userId == GuestId ? GuestLastReadOnUtc : HostLastReadOnUtc;

    /// <summary>
    /// Returns <see langword="true"/> and records the e-mail when the participant may be e-mailed about a new message:
    /// at most once per <paramref name="throttle"/>.
    /// </summary>
    public bool TryRecordEmail(Guid userId, DateTime utcNow, TimeSpan throttle)
    {
        DateTime? last = userId == GuestId ? GuestLastEmailedOnUtc : HostLastEmailedOnUtc;

        if (last is not null && utcNow - last.Value < throttle)
        {
            return false;
        }

        if (userId == GuestId)
        {
            GuestLastEmailedOnUtc = utcNow;
        }
        else
        {
            HostLastEmailedOnUtc = utcNow;
        }

        return true;
    }

    private void Touch(DateTime utcNow)
    {
        if (utcNow > LastMessageOnUtc)
        {
            LastMessageOnUtc = utcNow;
        }
    }
}
