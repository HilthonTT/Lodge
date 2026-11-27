using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Hosting.Bookings.Decline;

internal sealed class DeclineBookingRequestCommandHandler(
    IHostAccess hostAccess,
    IBookingRepository bookingRepository,
    IApartmentRepository apartmentRepository,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<DeclineBookingRequestCommand>
{
    public async Task<Result> Handle(DeclineBookingRequestCommand request, CancellationToken cancellationToken)
    {
        Result<Booking> booking = await HostBookings.GetManagedBookingAsync(
            hostAccess,
            bookingRepository,
            apartmentRepository,
            request.BookingId,
            cancellationToken);

        return booking.IsFailure ? booking : booking.Value.Decline(dateTimeProvider.UtcNow);
    }
}
