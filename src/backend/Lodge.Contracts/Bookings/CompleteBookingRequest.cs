namespace Lodge.Contracts.Bookings;

/// <summary>
/// Represents the complete booking request.
/// </summary>
/// <param name="UserId">The user identifier.</param>
public sealed record CompleteBookingRequest(Guid UserId);
