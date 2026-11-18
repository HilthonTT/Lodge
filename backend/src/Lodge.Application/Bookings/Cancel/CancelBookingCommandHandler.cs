using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Bookings.Cancel;

internal sealed class CancelBookingCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IBookingRepository bookingRepository,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CancelBookingCommand>
{
    public async Task<Result> Handle(CancelBookingCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId != userIdentifierProvider.UserId)
        {
            return Result.Failure(UserErrors.InvalidPermissions);
        }

        Booking? booking = await bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);

        if (booking is null)
        {
            return Result.Failure(BookingErrors.NotFound(request.BookingId));
        }

        if (booking.UserId != request.UserId)
        {
            return Result.Failure(UserErrors.InvalidPermissions);
        }

        return booking.Cancel(dateTimeProvider.UtcNow);
    }
}
