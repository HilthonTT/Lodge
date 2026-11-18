using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Bookings.Complete;

/// <summary>
/// Published to the message broker when a booking has been completed.
/// </summary>
public sealed record BookingCompletedIntegrationEvent(Guid BookingId) : IIntegrationEvent;
