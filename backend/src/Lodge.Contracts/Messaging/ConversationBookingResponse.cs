namespace Lodge.Contracts.Messaging;

/// <param name="Status">
/// The booking status name: <c>PendingApproval</c>, <c>Reserved</c>, <c>Confirmed</c>, <c>Rejected</c>,
/// <c>Cancelled</c>, <c>Completed</c> or <c>Expired</c>.
/// </param>
public sealed record ConversationBookingResponse(Guid Id, string Status, DateOnly DurationStart, DateOnly DurationEnd);
