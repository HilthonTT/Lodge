namespace Lodge.Contracts.Messaging;

/// <param name="Role"><c>"Host"</c> or <c>"Guest"</c>: the other participant's role in this conversation.</param>
public sealed record ConversationParticipantResponse(Guid Id, string FirstName, string? AvatarUrl, string Role);
