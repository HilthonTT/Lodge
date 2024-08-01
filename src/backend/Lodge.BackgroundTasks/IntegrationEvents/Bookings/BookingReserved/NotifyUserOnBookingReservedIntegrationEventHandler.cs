using Lodge.Application.Abstractions.Notifications;
using Lodge.Application.Bookings.Reserve;
using Lodge.BackgroundTasks.Abstractions.Messaging;
using Lodge.Contracts.Emails;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Exceptions;
using Lodge.Domain.Users;

namespace Lodge.BackgroundTasks.IntegrationEvents.Bookings.BookingReserved;

internal sealed class NotifyUserOnBookingReservedIntegrationEventHandler(
    IBookingRepository bookingRepository,
    IUserRepository userRepository,
    IEmailNotificationService emailNotificationService): IIntegrationEventHandler<BookingReservedIntegrationEvent>
{
    public async Task Handle(BookingReservedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        Booking? booking = await bookingRepository.GetByIdAsync(notification.BookingId, cancellationToken);

        if (booking is null)
        {
            throw new DomainException(BookingErrors.NotFound(notification.BookingId));
        }

        User? user = await userRepository.GetByIdAsync(booking.UserId, cancellationToken);
        if (user is null)
        {
            throw new DomainException(UserErrors.NotFound(booking.UserId));
        }

        var bookingReservedEmail = new BookingReservedEmail(user.Email, user.FullName);

        await emailNotificationService.SendBookingReservedEmailAsync(bookingReservedEmail, cancellationToken);
    }
}
