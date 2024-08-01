using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Bookings.Reserve;

/// <summary>
/// Represents the reserve booking command.
/// </summary>
/// <param name="ApartmentId">The apartment identifier.</param>
/// <param name="UserId">The user identifier.</param>
/// <param name="StartDate">The start date.</param>
/// <param name="EndDate">The end date.</param>
public sealed record ReserveBookingCommand(
    Guid ApartmentId, 
    Guid UserId,
    DateOnly StartDate, 
    DateOnly EndDate) : ICommand<Guid>;
