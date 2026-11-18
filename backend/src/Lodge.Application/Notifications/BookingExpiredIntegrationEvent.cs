using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Notifications;

public sealed record BookingExpiredIntegrationEvent(Guid BookingId) : IIntegrationEvent;
