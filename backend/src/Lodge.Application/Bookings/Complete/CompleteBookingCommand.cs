using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Bookings.Complete;

/// <summary>
/// Represents the complete booking command. Admin only: authorization is enforced by the endpoint policy.
/// </summary>
/// <param name="BookingId">The booking identifier.</param>
public sealed record CompleteBookingCommand(Guid BookingId) : ICommand;
