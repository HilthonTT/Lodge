using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Bookings.Confirm;

/// <summary>
/// Published to the message broker when a booking has been confirmed (paid).
/// </summary>
public sealed record BookingConfirmedIntegrationEvent(Guid BookingId) : IIntegrationEvent;
