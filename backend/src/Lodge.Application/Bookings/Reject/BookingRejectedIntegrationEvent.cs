using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Bookings.Reject;

/// <summary>
/// Published to the message broker when a booking has been rejected.
/// </summary>
public sealed record BookingRejectedIntegrationEvent(Guid BookingId) : IIntegrationEvent;
