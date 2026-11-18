using Lodge.Application.Abstractions.Notifications;
using Lodge.Application.Bookings.Reject;
using Lodge.BackgroundTasks.Abstractions.Messaging;
using Lodge.Contracts.Emails;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Exceptions;
using Lodge.Domain.Users;

namespace Lodge.BackgroundTasks.IntegrationEvents.Bookings.BookingRejected;

/// <summary>
/// Represents the <see cref="BookingRejectedIntegrationEvent"/> handler.
/// </summary>
/// <param name="bookingRepository">The booking repository.</param>
/// <param name="userRepository">The user repository.</param>
/// <param name="emailNotificationService">The email notification service.</param>
internal sealed class NotifyUserOnBookingRejectedEventHandler(
    IBookingRepository bookingRepository,
    IUserRepository userRepository,
    IEmailNotificationService emailNotificationService) : IIntegrationEventHandler<BookingRejectedIntegrationEvent>
{
    /// <inheritdoc />
    public async Task Handle(BookingRejectedIntegrationEvent notification, CancellationToken cancellationToken)
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

        var bookingRejectedEmail = new BookingRejectedEmail(user.Email, user.FullName);

        await emailNotificationService.SendBookingRejectedEmailAsync(bookingRejectedEmail, cancellationToken);
    }
}
