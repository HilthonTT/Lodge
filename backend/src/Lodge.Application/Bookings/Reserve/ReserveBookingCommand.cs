using Lodge.Application.Abstractions.Idempotency;

namespace Lodge.Application.Bookings.Reserve;

/// <summary>
/// Books a listed apartment: <c>Reserved</c> for instant-book listings, otherwise a <c>PendingApproval</c> request
/// that holds the dates until the host responds.
/// </summary>
/// <param name="RequestId">The request (idempotency) identifier.</param>
/// <param name="ApartmentId">The apartment identifier.</param>
/// <param name="UserId">The user identifier.</param>
/// <param name="StartDate">The check-in date.</param>
/// <param name="EndDate">The check-out date.</param>
/// <param name="GuestCount">The number of guests (1 to the listing's maximum).</param>
/// <param name="MessageToHost">An optional message to the host.</param>
public sealed record ReserveBookingCommand(
    Guid RequestId,
    Guid ApartmentId,
    Guid UserId,
    DateOnly StartDate,
    DateOnly EndDate,
    int GuestCount,
    string? MessageToHost = null) : IdempotentCommand<Guid>(RequestId);
