using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Notifications;

public sealed record ReviewRepliedIntegrationEvent(Guid ReviewId) : IIntegrationEvent;
