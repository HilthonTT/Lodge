using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Notifications;

public sealed record PayoutPaidIntegrationEvent(Guid PayoutId) : IIntegrationEvent;
