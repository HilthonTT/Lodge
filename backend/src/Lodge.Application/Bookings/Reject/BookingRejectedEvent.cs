using MediatR;

namespace Lodge.Application.Bookings.Reject;

/// <summary>
/// Represents the event that is triggered when a booking is rejected.
/// </summary>
/// <param name="BookingId">The booking identifier.</param>
/// <param name="UserId">The user identifier.</param>
internal sealed record BookingRejectedEvent(Guid BookingId, Guid UserId) : INotification;
