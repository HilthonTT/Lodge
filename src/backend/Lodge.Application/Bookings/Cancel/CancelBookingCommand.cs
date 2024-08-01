using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Bookings.Cancel;

/// <summary>
/// Represents the cancel booking command.
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="BookingId">The booking identifier.</param>
public sealed record CancelBookingCommand(Guid UserId, Guid BookingId) : ICommand;
