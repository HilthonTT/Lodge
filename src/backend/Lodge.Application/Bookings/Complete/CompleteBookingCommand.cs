using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Bookings.Complete;

/// <summary>
/// Represents the complete booking command.
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="BookingId">The booking identifier.</param>
public sealed record CompleteBookingCommand(Guid UserId, Guid BookingId) : ICommand;
