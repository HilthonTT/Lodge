using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Payments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Bookings.Cancel;

/// <summary>
/// The guest cancels their booking. For a paid booking the refund follows the cancellation policy: it is stored on the
/// booking and sent asynchronously (outbox → integration event → refund processor), and the host payout is adjusted.
/// </summary>
internal sealed class CancelBookingCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IBookingRepository bookingRepository,
    BookingPayments bookingPayments,
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

        Result<RefundQuote> cancelled = booking.Cancel(dateTimeProvider.UtcNow);

        if (cancelled.IsFailure)
        {
            return cancelled;
        }

        await bookingPayments.AdjustPayoutAsync(booking, cancelled.Value, cancellationToken);

        return Result.Success();
    }
}
