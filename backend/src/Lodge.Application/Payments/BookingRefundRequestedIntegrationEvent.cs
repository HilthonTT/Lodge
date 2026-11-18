using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Payments;

/// <summary>
/// Published to the message broker when a cancelled, paid booking has a refund to send.
/// </summary>
public sealed record BookingRefundRequestedIntegrationEvent(Guid BookingId) : IIntegrationEvent;
