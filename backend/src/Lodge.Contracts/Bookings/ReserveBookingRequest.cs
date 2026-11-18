namespace Lodge.Contracts.Bookings;

/// <summary>
/// Represents the reserve booking request.
/// </summary>
/// <param name="ApartmentId">The apartment identifier.</param>
/// <param name="UserId">The user identifier.</param>
/// <param name="StartDate">The check-in date.</param>
/// <param name="EndDate">The check-out date.</param>
/// <param name="GuestCount">The number of guests (1 to the listing's maximum).</param>
/// <param name="MessageToHost">An optional message to the host (at most 1000 characters).</param>
public sealed record ReserveBookingRequest(
    Guid ApartmentId,
    Guid UserId,
    DateOnly StartDate,
    DateOnly EndDate,
    int GuestCount,
    string? MessageToHost = null);
