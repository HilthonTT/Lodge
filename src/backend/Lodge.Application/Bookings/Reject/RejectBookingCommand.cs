using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Bookings.Reject;

/// <summary>
/// Represents the reject booking command.
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="BookingId">The booking identifier.</param>
public sealed record RejectBookingCommand(Guid UserId, Guid BookingId) : ICommand;
