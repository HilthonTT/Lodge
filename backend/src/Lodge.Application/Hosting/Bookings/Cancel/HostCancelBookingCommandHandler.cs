using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Payments;
using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Hosting.Bookings.Cancel;

internal sealed class HostCancelBookingCommandHandler(
    IHostAccess hostAccess,
    IBookingRepository bookingRepository,
    IApartmentRepository apartmentRepository,
    BookingPayments bookingPayments,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<HostCancelBookingCommand>
{
    public async Task<Result> Handle(HostCancelBookingCommand request, CancellationToken cancellationToken)
    {
        Result<Booking> booking = await HostBookings.GetManagedBookingAsync(
            hostAccess,
            bookingRepository,
            apartmentRepository,
            request.BookingId,
            cancellationToken);

        if (booking.IsFailure)
        {
            return booking;
        }

        Result<RefundQuote> cancelled = booking.Value.CancelByHost(dateTimeProvider.UtcNow);

        if (cancelled.IsFailure)
        {
            return cancelled;
        }

        await bookingPayments.AdjustPayoutAsync(booking.Value, cancelled.Value, cancellationToken);

        return Result.Success();
    }
}
