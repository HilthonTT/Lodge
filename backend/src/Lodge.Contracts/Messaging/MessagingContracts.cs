namespace Lodge.Contracts.Messaging;

/// <summary>
/// A guest's inquiry about a listing (<c>POST /conversations</c>).
/// </summary>
public sealed record StartConversationRequest(Guid ApartmentId, string Message);

/// <summary>
/// A new message in a conversation (<c>POST /conversations/{id}/messages</c>).
/// </summary>
public sealed record SendMessageRequest(string Body);

/// <summary>
/// The conversation the inquiry was posted to (new or existing) and the posted message.
/// </summary>
public sealed record SentMessageResponse(Guid ConversationId, MessageResponse Message);

/// <summary>
/// A message. <see cref="Type"/> is <c>"User"</c> or <c>"System"</c>; system messages have no sender, a
/// <see cref="SystemEvent"/> code (localised by the frontend) and an English fallback <see cref="Body"/>.
/// </summary>
public sealed record MessageResponse
{
    public Guid Id { get; init; }

    public Guid ConversationId { get; init; }

    public Guid? SenderId { get; init; }

    public string Type { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;

    public string? SystemEvent { get; init; }

    public Guid? BookingId { get; init; }

    public DateTime SentOnUtc { get; init; }
}

/// <summary>
/// One page of messages in chronological order (oldest first). Pass <see cref="NextBefore"/> as <c>before</c> to get
/// the previous (older) page; it is null when there is none.
/// </summary>
public sealed record MessagePageResponse(IReadOnlyList<MessageResponse> Items, bool HasMore, Guid? NextBefore);

/// <summary>
/// A conversation as listed in the inbox.
/// </summary>
public sealed record ConversationResponse
{
    public Guid Id { get; init; }

    public ConversationApartmentResponse Apartment { get; init; } = null!;

    public ConversationParticipantResponse OtherParticipant { get; init; } = null!;

    /// <summary>
    /// Gets the latest message, or null for a conversation without messages.
    /// </summary>
    public LastMessageResponse? LastMessage { get; init; }

    /// <summary>
    /// Gets the number of messages from the other participant or the system after the caller's read marker.
    /// </summary>
    public int UnreadCount { get; init; }

    /// <summary>
    /// Gets the guest's most recent booking of the listing, or null.
    /// </summary>
    public ConversationBookingResponse? LatestBooking { get; init; }

    public DateTime LastMessageOnUtc { get; init; }
}

public sealed record ConversationApartmentResponse(Guid Id, string Name, string? ImageUrl);

/// <param name="Role"><c>"Host"</c> or <c>"Guest"</c>: the other participant's role in this conversation.</param>
public sealed record ConversationParticipantResponse(Guid Id, string FirstName, string? AvatarUrl, string Role);

/// <param name="Type"><c>"User"</c> or <c>"System"</c>.</param>
public sealed record LastMessageResponse(string Body, string Type, DateTime SentOnUtc, Guid? SenderId, string? SystemEvent);

/// <param name="Status">
/// The booking status name: <c>PendingApproval</c>, <c>Reserved</c>, <c>Confirmed</c>, <c>Rejected</c>,
/// <c>Cancelled</c>, <c>Completed</c> or <c>Expired</c>.
/// </param>
public sealed record ConversationBookingResponse(Guid Id, string Status, DateOnly DurationStart, DateOnly DurationEnd);

public sealed record UnreadCountResponse(int Count);
