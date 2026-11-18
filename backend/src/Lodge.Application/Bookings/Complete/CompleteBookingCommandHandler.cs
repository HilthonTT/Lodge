using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Bookings.Complete;

internal sealed class CompleteBookingCommandHandler(
    IBookingRepository bookingRepository,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CompleteBookingCommand>
{
    public async Task<Result> Handle(CompleteBookingCommand request, CancellationToken cancellationToken)
    {
        Booking? booking = await bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);

        if (booking is null)
        {
            return Result.Failure(BookingErrors.NotFound(request.BookingId));
        }

        return booking.Complete(dateTimeProvider.UtcNow);
    }
}
