using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Notifications;

public sealed record MessageSentIntegrationEvent(Guid MessageId) : IIntegrationEvent;
