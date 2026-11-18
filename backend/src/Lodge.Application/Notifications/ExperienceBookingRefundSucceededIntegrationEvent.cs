using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Notifications;

public sealed record ExperienceBookingRefundSucceededIntegrationEvent(Guid ExperienceBookingId) : IIntegrationEvent;
