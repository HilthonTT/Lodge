using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Notifications;

public sealed record ExperienceBookingRefundRequestedIntegrationEvent(Guid ExperienceBookingId) : IIntegrationEvent;
