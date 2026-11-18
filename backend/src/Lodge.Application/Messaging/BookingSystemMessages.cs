using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Abstractions.Realtime;
using Lodge.Domain.Bookings;
using Lodge.Domain.Bookings.Events;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Messaging;

namespace Lodge.Application.Messaging;

/// <summary>
/// Posts a system message in the guest's conversation with the host on every booking transition (dispatched from the
/// outbox). The "requested"/"reserved" messages are posted with the booking itself
/// (<see cref="ConversationService.OpenForNewBookingAsync"/>).
/// </summary>
internal sealed class PostSystemMessageOnBookingEventDomainEventHandler(
    IBookingRepository bookingRepository,
    ConversationService conversationService,
    IUnitOfWork unitOfWork,
    IRealtimeOutbox realtimeOutbox,
    IDateTimeProvider dateTimeProvider) :
    IDomainEventHandler<BookingAcceptedDomainEvent>,
    IDomainEventHandler<BookingRejectedDomainEvent>,
    IDomainEventHandler<BookingExpiredDomainEvent>,
    IDomainEventHandler<BookingConfirmedDomainEvent>,
    IDomainEventHandler<BookingCancelledDomainEvent>,
    IDomainEventHandler<BookingDatesChangedDomainEvent>
{
    public Task Handle(BookingAcceptedDomainEvent notification, CancellationToken cancellationToken) =>
        PostAsync(notification.BookingId, _ => SystemEvents.BookingAccepted, cancellationToken);

    public Task Handle(BookingRejectedDomainEvent notification, CancellationToken cancellationToken) =>
        PostAsync(notification.BookingId, _ => SystemEvents.BookingDeclined, cancellationToken);

    public Task Handle(BookingExpiredDomainEvent notification, CancellationToken cancellationToken) =>
        PostAsync(notification.BookingId, _ => SystemEvents.BookingExpired, cancellationToken);

    public Task Handle(BookingConfirmedDomainEvent notification, CancellationToken cancellationToken) =>
        PostAsync(notification.BookingId, _ => SystemEvents.BookingConfirmed, cancellationToken);

    public Task Handle(BookingCancelledDomainEvent notification, CancellationToken cancellationToken) =>
        PostAsync(
            notification.BookingId,
            booking => booking.CancelledByHost ? SystemEvents.BookingCancelledByHost : SystemEvents.BookingCancelledByGuest,
            cancellationToken);

    public Task Handle(BookingDatesChangedDomainEvent notification, CancellationToken cancellationToken) =>
        PostAsync(notification.BookingId, _ => SystemEvents.BookingDatesChanged, cancellationToken);

    private async Task PostAsync(Guid bookingId, Func<Booking, string> systemEvent, CancellationToken cancellationToken)
    {
        Booking? booking = await bookingRepository.GetByIdAsync(bookingId, cancellationToken);

        if (booking is null)
        {
            return;
        }

        Message? message = await conversationService.PostSystemMessageAsync(
            booking,
            systemEvent(booking),
            dateTimeProvider.UtcNow,
            cancellationToken);

        if (message is null)
        {
            return;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await realtimeOutbox.FlushAsync(cancellationToken);
    }
}
