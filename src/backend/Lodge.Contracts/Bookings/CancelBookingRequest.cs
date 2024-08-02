namespace Lodge.Contracts.Bookings;

/// <summary>
/// Represents the cancel booking request.
/// </summary>
/// <param name="UserId">The user identifier.</param>
public sealed record CancelBookingRequest(Guid UserId);
