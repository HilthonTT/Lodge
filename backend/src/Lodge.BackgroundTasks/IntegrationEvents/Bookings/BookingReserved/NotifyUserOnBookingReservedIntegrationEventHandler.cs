using Lodge.Application.Abstractions.Notifications;
using Lodge.Application.Bookings.Reserve;
using Lodge.BackgroundTasks.Abstractions.Messaging;
using Lodge.Contracts.Emails;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Exceptions;
using Lodge.Domain.Users;

namespace Lodge.BackgroundTasks.IntegrationEvents.Bookings.BookingReserved;

/// <summary>
/// Represents the <see cref="BookingReservedIntegrationEvent"/> handler.
/// </summary>
/// <param name="bookingRepository">The booking repository.</param>
/// <param name="userRepository">The user repository.</param>
/// <param name="emailNotificationService">The email notification service.</param>
internal sealed class NotifyUserOnBookingReservedIntegrationEventHandler(
    IBookingRepository bookingRepository,
    IUserRepository userRepository,
    IEmailNotificationService emailNotificationService): IIntegrationEventHandler<BookingReservedIntegrationEvent>
{
    /// <inheritdoc />
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
