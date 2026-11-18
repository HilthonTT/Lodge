using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Notifications;

public sealed record BookingRefundSucceededIntegrationEvent(Guid BookingId) : IIntegrationEvent;
