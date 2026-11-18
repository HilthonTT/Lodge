using Lodge.Application.Abstractions.Idempotency;

namespace Lodge.Application.Bookings.Reserve;

/// <summary>
/// Represents the reserve booking command.
/// </summary>
/// <param name="RequestId">The request identifier.</param>
/// <param name="ApartmentId">The apartment identifier.</param>
/// <param name="UserId">The user identifier.</param>
/// <param name="StartDate">The start date.</param>
/// <param name="EndDate">The end date.</param>
public sealed record ReserveBookingCommand(
    Guid RequestId,
    Guid ApartmentId, 
    Guid UserId,
    DateOnly StartDate, 
    DateOnly EndDate) : IdempotentCommand<Guid>(RequestId);
