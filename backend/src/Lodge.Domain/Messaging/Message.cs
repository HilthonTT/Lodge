using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Messaging.Events;

namespace Lodge.Domain.Messaging;

/// <summary>
/// A message of a <see cref="Conversation"/>: written by a participant (<see cref="MessageType.User"/>) or posted by
/// Lodge on a booking event (<see cref="MessageType.System"/>, without a sender).
/// </summary>
public sealed class Message : Entity
{
    public const int BodyMaxLength = 2000;

    public const int SystemEventMaxLength = 50;

    /// <summary>
    /// The default number of messages per page.
    /// </summary>
    public const int DefaultPageSize = 30;

    public const int MaxPageSize = 100;

    private Message(
        Guid id,
        Guid conversationId,
        Guid? senderId,
        MessageType type,
        string body,
        string? systemEvent,
        Guid? bookingId,
        DateTime sentOnUtc)
        : base(id)
    {
        ConversationId = conversationId;
        SenderId = senderId;
        Type = type;
        Body = body;
        SystemEvent = systemEvent;
        BookingId = bookingId;
        SentOnUtc = sentOnUtc;
    }

    /// <remarks>
    /// Required by EF Core.
    /// </remarks>
    private Message()
    {
    }

    public Guid ConversationId { get; private set; }

    /// <summary>
    /// Gets the author, or null for system messages.
    /// </summary>
    public Guid? SenderId { get; private set; }

    public MessageType Type { get; private set; }

    public string Body { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the code of a system message (see <see cref="SystemEvents"/>), else null.
    /// </summary>
    public string? SystemEvent { get; private set; }

    /// <summary>
    /// Gets the booking a system message (or a booking's message to the host) is about, else null.
    /// </summary>
    public Guid? BookingId { get; private set; }

    public DateTime SentOnUtc { get; private set; }

    /// <summary>
    /// Validates and normalises a message body: trimmed, not empty, at most <see cref="BodyMaxLength"/> characters.
    /// </summary>
    public static Result<string> NormalizeBody(string? body)
    {
        string trimmed = body?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            return Result.Failure<string>(MessagingErrors.EmptyMessage);
        }

        if (trimmed.Length > BodyMaxLength)
        {
            return Result.Failure<string>(MessagingErrors.MessageTooLong);
        }

        return trimmed;
    }

    internal static Result<Message> CreateUser(Conversation conversation, Guid senderId, string? body, DateTime utcNow)
    {
        Result<string> normalized = NormalizeBody(body);

        if (normalized.IsFailure)
        {
            return Result.Failure<Message>(normalized.Error);
        }

        var message = new Message(
            Guid.NewGuid(),
            conversation.Id,
            senderId,
            MessageType.User,
            normalized.Value,
            null,
            null,
            utcNow);

        message.RaiseDomainEvent(new MessageSentDomainEvent(message.Id));

        return message;
    }

    internal static Message CreateSystem(
        Conversation conversation,
        string systemEvent,
        Guid? bookingId,
        string fallbackBody,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(systemEvent);

        string body = fallbackBody.Trim();

        return new Message(
            Guid.NewGuid(),
            conversation.Id,
            null,
            MessageType.System,
            body.Length > BodyMaxLength ? body[..BodyMaxLength] : body,
            systemEvent,
            bookingId,
            utcNow);
    }

    /// <summary>
    /// Links a guest's message to the booking it was written with.
    /// </summary>
    public void AttachToBooking(Guid bookingId) => BookingId = bookingId;
}
