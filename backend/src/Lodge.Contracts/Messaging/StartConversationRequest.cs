namespace Lodge.Contracts.Messaging;

/// <summary>
/// A guest's inquiry about a listing (<c>POST /conversations</c>).
/// </summary>
public sealed record StartConversationRequest(Guid ApartmentId, string Message);
