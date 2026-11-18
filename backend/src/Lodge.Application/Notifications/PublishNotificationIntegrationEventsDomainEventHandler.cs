using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Bookings.Events;
using Lodge.Domain.Experiences.Events;
using Lodge.Domain.Messaging.Events;
using Lodge.Domain.Payouts.Events;
using Lodge.Domain.Reviews.Events;

namespace Lodge.Application.Notifications;

/// <summary>
/// Publishes the integration events behind the notifications, messages e-mails and experience refunds.
/// </summary>
internal sealed class PublishNotificationIntegrationEventsDomainEventHandler(IIntegrationEventPublisher publisher) :
    IDomainEventHandler<BookingRequestedDomainEvent>,
    IDomainEventHandler<BookingAcceptedDomainEvent>,
    IDomainEventHandler<BookingExpiredDomainEvent>,
    IDomainEventHandler<BookingDatesChangedDomainEvent>,
    IDomainEventHandler<BookingRefundSucceededDomainEvent>,
    IDomainEventHandler<ReviewPublishedDomainEvent>,
    IDomainEventHandler<GuestReviewPublishedDomainEvent>,
    IDomainEventHandler<ReviewRepliedDomainEvent>,
    IDomainEventHandler<PayoutPaidDomainEvent>,
    IDomainEventHandler<MessageSentDomainEvent>,
    IDomainEventHandler<ExperienceBookingConfirmedDomainEvent>,
    IDomainEventHandler<ExperienceBookingRefundRequestedDomainEvent>,
    IDomainEventHandler<ExperienceBookingRefundSucceededDomainEvent>
{
    public Task Handle(BookingRequestedDomainEvent notification, CancellationToken cancellationToken) =>
        publisher.PublishAsync(new BookingRequestedIntegrationEvent(notification.BookingId), cancellationToken);

    public Task Handle(BookingAcceptedDomainEvent notification, CancellationToken cancellationToken) =>
        publisher.PublishAsync(new BookingAcceptedIntegrationEvent(notification.BookingId), cancellationToken);

    public Task Handle(BookingExpiredDomainEvent notification, CancellationToken cancellationToken) =>
        publisher.PublishAsync(new BookingExpiredIntegrationEvent(notification.BookingId), cancellationToken);

    public Task Handle(BookingDatesChangedDomainEvent notification, CancellationToken cancellationToken) =>
        publisher.PublishAsync(
            new BookingDatesChangedIntegrationEvent(notification.BookingId, notification.PreviousStart, notification.PreviousEnd),
            cancellationToken);

    public Task Handle(BookingRefundSucceededDomainEvent notification, CancellationToken cancellationToken) =>
        publisher.PublishAsync(new BookingRefundSucceededIntegrationEvent(notification.BookingId), cancellationToken);

    public Task Handle(ReviewPublishedDomainEvent notification, CancellationToken cancellationToken) =>
        publisher.PublishAsync(new ReviewPublishedIntegrationEvent(notification.ReviewId), cancellationToken);

    public Task Handle(GuestReviewPublishedDomainEvent notification, CancellationToken cancellationToken) =>
        publisher.PublishAsync(new GuestReviewPublishedIntegrationEvent(notification.GuestReviewId), cancellationToken);

    public Task Handle(ReviewRepliedDomainEvent notification, CancellationToken cancellationToken) =>
        publisher.PublishAsync(new ReviewRepliedIntegrationEvent(notification.ReviewId), cancellationToken);

    public Task Handle(PayoutPaidDomainEvent notification, CancellationToken cancellationToken) =>
        publisher.PublishAsync(new PayoutPaidIntegrationEvent(notification.PayoutId), cancellationToken);

    public Task Handle(MessageSentDomainEvent notification, CancellationToken cancellationToken) =>
        publisher.PublishAsync(new MessageSentIntegrationEvent(notification.MessageId), cancellationToken);

    public Task Handle(ExperienceBookingConfirmedDomainEvent notification, CancellationToken cancellationToken) =>
        publisher.PublishAsync(
            new ExperienceBookingConfirmedIntegrationEvent(notification.ExperienceBookingId),
            cancellationToken);

    public Task Handle(ExperienceBookingRefundRequestedDomainEvent notification, CancellationToken cancellationToken) =>
        publisher.PublishAsync(
            new ExperienceBookingRefundRequestedIntegrationEvent(notification.ExperienceBookingId),
            cancellationToken);

    public Task Handle(ExperienceBookingRefundSucceededDomainEvent notification, CancellationToken cancellationToken) =>
        publisher.PublishAsync(
            new ExperienceBookingRefundSucceededIntegrationEvent(notification.ExperienceBookingId),
            cancellationToken);
}
