namespace Lodge.Contracts.Bookings;

/// <summary>
/// Represents the confirm booking request.
/// </summary>
/// <param name="UserId">The user identifier.</param>
public sealed record ConfirmBookingRequest(Guid UserId);
