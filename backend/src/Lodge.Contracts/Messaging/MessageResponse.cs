namespace Lodge.Contracts.Messaging;

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
