using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Bookings.Reserve;

/// <summary>
/// Published to the message broker when a booking has been reserved.
/// </summary>
public sealed record BookingReservedIntegrationEvent(Guid BookingId) : IIntegrationEvent;
