using Lodge.Application.Abstractions.Notifications;
using Lodge.Application.Notifications;
using Lodge.Application.Bookings.Cancel;
using Lodge.BackgroundTasks.Abstractions.Messaging;
using Lodge.Contracts.Emails;
using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Exceptions;
using Lodge.Domain.Notifications;
using Lodge.Domain.Users;

namespace Lodge.BackgroundTasks.IntegrationEvents.Bookings.BookingCancelled;

/// <summary>
/// Tells the guest that the booking was cancelled, with the refund of a paid booking.
/// </summary>
internal sealed class NotifyUserOnBookingCancelledEventHandler(
    IBookingRepository bookingRepository,
    IApartmentRepository apartmentRepository,
    IUserRepository userRepository,
    IEmailNotificationService emailNotificationService,
    INotificationPreferencesProvider preferencesProvider) : IIntegrationEventHandler<BookingCancelledIntegrationEvent>
{
    public async Task Handle(BookingCancelledIntegrationEvent notification, CancellationToken cancellationToken)
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

        // Reservation e-mails respect the guest's notification preferences.
        if (!await preferencesProvider.AllowsEmailAsync(user.Id, NotificationCategory.Reservations, cancellationToken))
        {
            return;
        }

        Apartment? apartment = await apartmentRepository.GetByIdAsync(booking.ApartmentId, cancellationToken);

        var bookingCancelledEmail = new BookingCancelledEmail(
            user.Email,
            user.FullName,
            apartment?.Name.Value,
            booking.RefundAmount,
            booking.TotalPrice.Currency.Code,
            booking.CancelledByHost,
            booking.RefundStatus == RefundStatus.Manual);

        await emailNotificationService.SendBookingCancelledEmailAsync(bookingCancelledEmail, cancellationToken);
    }
}
