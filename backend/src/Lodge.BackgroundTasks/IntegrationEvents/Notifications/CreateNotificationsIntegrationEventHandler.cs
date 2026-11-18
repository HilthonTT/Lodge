using Lodge.Application.Bookings.Cancel;
using Lodge.Application.Bookings.Confirm;
using Lodge.Application.Bookings.Reject;
using Lodge.Application.Notifications;
using Lodge.BackgroundTasks.Abstractions.Messaging;

namespace Lodge.BackgroundTasks.IntegrationEvents.Notifications;

/// <summary>
/// Creates the in-app notifications (pushed over SignalR) of the platform's events, and the "new message" e-mails.
/// Every notification is created once per source event, so redelivered messages are harmless.
/// </summary>
internal sealed class CreateNotificationsIntegrationEventHandler(IUserNotifier userNotifier) :
    IIntegrationEventHandler<BookingRequestedIntegrationEvent>,
    IIntegrationEventHandler<BookingAcceptedIntegrationEvent>,
    IIntegrationEventHandler<BookingRejectedIntegrationEvent>,
    IIntegrationEventHandler<BookingExpiredIntegrationEvent>,
    IIntegrationEventHandler<BookingConfirmedIntegrationEvent>,
    IIntegrationEventHandler<BookingCancelledIntegrationEvent>,
    IIntegrationEventHandler<BookingDatesChangedIntegrationEvent>,
    IIntegrationEventHandler<BookingRefundSucceededIntegrationEvent>,
    IIntegrationEventHandler<ReviewPublishedIntegrationEvent>,
    IIntegrationEventHandler<GuestReviewPublishedIntegrationEvent>,
    IIntegrationEventHandler<ReviewRepliedIntegrationEvent>,
    IIntegrationEventHandler<PayoutPaidIntegrationEvent>,
    IIntegrationEventHandler<ExperienceBookingConfirmedIntegrationEvent>,
    IIntegrationEventHandler<ExperienceBookingRefundSucceededIntegrationEvent>,
    IIntegrationEventHandler<MessageSentIntegrationEvent>
{
    public Task Handle(BookingRequestedIntegrationEvent notification, CancellationToken cancellationToken) =>
        userNotifier.BookingRequestedAsync(notification.BookingId, cancellationToken);

    public Task Handle(BookingAcceptedIntegrationEvent notification, CancellationToken cancellationToken) =>
        userNotifier.BookingAcceptedAsync(notification.BookingId, cancellationToken);

    public Task Handle(BookingRejectedIntegrationEvent notification, CancellationToken cancellationToken) =>
        userNotifier.BookingDeclinedAsync(notification.BookingId, cancellationToken);

    public Task Handle(BookingExpiredIntegrationEvent notification, CancellationToken cancellationToken) =>
        userNotifier.BookingExpiredAsync(notification.BookingId, cancellationToken);

    public Task Handle(BookingConfirmedIntegrationEvent notification, CancellationToken cancellationToken) =>
        userNotifier.BookingConfirmedAsync(notification.BookingId, cancellationToken);

    public Task Handle(BookingCancelledIntegrationEvent notification, CancellationToken cancellationToken) =>
        userNotifier.BookingCancelledAsync(notification.BookingId, cancellationToken);

    public Task Handle(BookingDatesChangedIntegrationEvent notification, CancellationToken cancellationToken) =>
        userNotifier.BookingDatesChangedAsync(
            notification.BookingId,
            notification.PreviousStart,
            notification.PreviousEnd,
            cancellationToken);

    public Task Handle(BookingRefundSucceededIntegrationEvent notification, CancellationToken cancellationToken) =>
        userNotifier.BookingRefundSucceededAsync(notification.BookingId, cancellationToken);

    public Task Handle(ReviewPublishedIntegrationEvent notification, CancellationToken cancellationToken) =>
        userNotifier.ReviewPublishedAsync(notification.ReviewId, cancellationToken);

    public Task Handle(GuestReviewPublishedIntegrationEvent notification, CancellationToken cancellationToken) =>
        userNotifier.GuestReviewPublishedAsync(notification.GuestReviewId, cancellationToken);

    public Task Handle(ReviewRepliedIntegrationEvent notification, CancellationToken cancellationToken) =>
        userNotifier.ReviewRepliedAsync(notification.ReviewId, cancellationToken);

    public Task Handle(PayoutPaidIntegrationEvent notification, CancellationToken cancellationToken) =>
        userNotifier.PayoutPaidAsync(notification.PayoutId, cancellationToken);

    public Task Handle(ExperienceBookingConfirmedIntegrationEvent notification, CancellationToken cancellationToken) =>
        userNotifier.ExperienceBookingConfirmedAsync(notification.ExperienceBookingId, cancellationToken);

    public Task Handle(ExperienceBookingRefundSucceededIntegrationEvent notification, CancellationToken cancellationToken) =>
        userNotifier.ExperienceRefundSucceededAsync(notification.ExperienceBookingId, cancellationToken);

    public Task Handle(MessageSentIntegrationEvent notification, CancellationToken cancellationToken) =>
        userNotifier.MessageSentAsync(notification.MessageId, cancellationToken);
}
