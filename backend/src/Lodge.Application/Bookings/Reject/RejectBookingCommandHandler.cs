using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Bookings.Reject;

internal sealed class RejectBookingCommandHandler(
    IBookingRepository bookingRepository,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<RejectBookingCommand>
{
    public async Task<Result> Handle(RejectBookingCommand request, CancellationToken cancellationToken)
    {
        Booking? booking = await bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);

        if (booking is null)
        {
            return Result.Failure(BookingErrors.NotFound(request.BookingId));
        }

        return booking.Reject(dateTimeProvider.UtcNow);
    }
}
