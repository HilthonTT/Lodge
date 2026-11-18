namespace Lodge.Contracts.Messaging;

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
