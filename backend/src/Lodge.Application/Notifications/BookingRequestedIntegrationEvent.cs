using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Notifications;

public sealed record BookingRequestedIntegrationEvent(Guid BookingId) : IIntegrationEvent;
