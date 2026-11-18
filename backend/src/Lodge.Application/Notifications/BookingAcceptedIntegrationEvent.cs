using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Notifications;

public sealed record BookingAcceptedIntegrationEvent(Guid BookingId) : IIntegrationEvent;
