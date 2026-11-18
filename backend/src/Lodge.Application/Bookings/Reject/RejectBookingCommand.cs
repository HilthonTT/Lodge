using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Bookings.Reject;

/// <summary>
/// Represents the reject booking command. Admin only: authorization is enforced by the endpoint policy.
/// </summary>
/// <param name="BookingId">The booking identifier.</param>
public sealed record RejectBookingCommand(Guid BookingId) : ICommand;
