namespace Lodge.Contracts.Messaging;

/// <param name="Type"><c>"User"</c> or <c>"System"</c>.</param>
public sealed record LastMessageResponse(string Body, string Type, DateTime SentOnUtc, Guid? SenderId, string? SystemEvent);
