using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Hosting.Bookings.Accept;

internal sealed class AcceptBookingRequestCommandHandler(
    IHostAccess hostAccess,
    IBookingRepository bookingRepository,
    IApartmentRepository apartmentRepository,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<AcceptBookingRequestCommand>
{
    public async Task<Result> Handle(AcceptBookingRequestCommand request, CancellationToken cancellationToken)
    {
        Result<Booking> booking = await HostBookings.GetManagedBookingAsync(
            hostAccess,
            bookingRepository,
            apartmentRepository,
            request.BookingId,
            cancellationToken);

        return booking.IsFailure ? booking : booking.Value.Accept(dateTimeProvider.UtcNow);
    }
}
