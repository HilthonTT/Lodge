using MediatR;

namespace Lodge.Application.Bookings.Cancel;

/// <summary>
/// Represents the event that is triggered when a booking is cancelled.
/// </summary>
/// <param name="BookingId">The booking identifier.</param>
internal sealed record BookingCancelledEvent(Guid BookingId) : INotification;
