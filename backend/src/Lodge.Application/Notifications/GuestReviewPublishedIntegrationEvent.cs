using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Notifications;

public sealed record GuestReviewPublishedIntegrationEvent(Guid GuestReviewId) : IIntegrationEvent;
