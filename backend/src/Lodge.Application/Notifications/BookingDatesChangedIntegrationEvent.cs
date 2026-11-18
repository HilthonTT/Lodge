using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Notifications;

public sealed record BookingDatesChangedIntegrationEvent(Guid BookingId, DateOnly PreviousStart, DateOnly PreviousEnd)
    : IIntegrationEvent;
