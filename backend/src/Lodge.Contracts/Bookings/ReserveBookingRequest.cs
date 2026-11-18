namespace Lodge.Contracts.Bookings;

/// <summary>
/// Represents the reserve booking request.
/// </summary>
/// <param name="ApartmentId">The apartment identifier.</param>
/// <param name="UserId">The user identifier.</param>
/// <param name="StartDate">The start date.</param>
/// <param name="EndDate">The end date.</param>
public sealed record ReserveBookingRequest(
    Guid ApartmentId,
    Guid UserId,
    DateOnly StartDate,
    DateOnly EndDate);
