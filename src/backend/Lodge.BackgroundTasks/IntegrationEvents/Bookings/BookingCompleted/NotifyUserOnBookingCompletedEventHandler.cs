using Lodge.Application.Abstractions.Notifications;
using Lodge.Application.Bookings.Complete;
using Lodge.BackgroundTasks.Abstractions.Messaging;
using Lodge.Contracts.Emails;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Exceptions;
using Lodge.Domain.Users;

namespace Lodge.BackgroundTasks.IntegrationEvents.Bookings.BookingCompleted;

/// <summary>
/// Represents the <see cref="BookingCompletedIntegrationEvent"/> handler.
/// </summary>
/// <param name="bookingRepository">The booking repository.</param>
/// <param name="userRepository">The user repository.</param>
/// <param name="emailNotificationService">The email notification service.</param>
internal sealed class NotifyUserOnBookingCompletedEventHandler(
    IBookingRepository bookingRepository,
    IUserRepository userRepository,
    IEmailNotificationService emailNotificationService) : IIntegrationEventHandler<BookingCompletedIntegrationEvent>
{
    /// <inheritdoc />
    public async Task Handle(BookingCompletedIntegrationEvent notification, CancellationToken cancellationToken)
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

        var bookingCompletedEmail = new BookingCompletedEmail(user.Email, user.FullName);

        await emailNotificationService.SendBookingCompletedEmailAsync(bookingCompletedEmail, cancellationToken);
    }
}
