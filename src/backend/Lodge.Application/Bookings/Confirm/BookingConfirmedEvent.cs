using MediatR;

namespace Lodge.Application.Bookings.Confirm;

/// <summary>
/// Represents the event that is triggered when a booking is confirmed.
/// </summary>
/// <param name="BookingId">The booking identifier.</param>
internal sealed record BookingConfirmedEvent(Guid BookingId) : INotification;
