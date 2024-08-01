using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Bookings.Confirm;

/// <summary>
/// Represents the confirm booking command.
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="BookingId">The booking identifier.</param>
public sealed record ConfirmBookingCommand(Guid UserId, Guid BookingId) : ICommand;
