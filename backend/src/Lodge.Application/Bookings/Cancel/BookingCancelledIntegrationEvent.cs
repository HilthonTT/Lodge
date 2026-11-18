using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Bookings.Cancel;

/// <summary>
/// Published to the message broker when a booking has been cancelled.
/// </summary>
public sealed record BookingCancelledIntegrationEvent(Guid BookingId) : IIntegrationEvent;
