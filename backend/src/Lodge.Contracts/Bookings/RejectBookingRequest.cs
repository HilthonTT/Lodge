namespace Lodge.Contracts.Bookings;

/// <summary>
/// Represents the reject booking request.
/// </summary>
/// <param name="UserId">The user identifier.</param>
public sealed record RejectBookingRequest(Guid UserId);
