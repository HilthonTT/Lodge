using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Core.Errors;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Payments;

internal sealed class CompleteSimulatedPaymentCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IBookingRepository bookingRepository,
    IPaymentModeProvider paymentModeProvider,
    BookingPayments bookingPayments) : ICommandHandler<CompleteSimulatedPaymentCommand>
{
    public async Task<Result> Handle(CompleteSimulatedPaymentCommand request, CancellationToken cancellationToken)
    {
        if (paymentModeProvider.Mode != PaymentMode.Simulated)
        {
            return Result.Failure(PaymentErrors.NotSimulated);
        }

        Booking? booking = await bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);

        if (booking is null)
        {
            return Result.Failure(BookingErrors.NotFound(request.BookingId));
        }

        if (booking.UserId != userIdentifierProvider.UserId)
        {
            return Result.Failure(UserErrors.InvalidPermissions);
        }

        if (booking.Status is not (BookingStatus.Reserved or BookingStatus.Confirmed or BookingStatus.Completed))
        {
            return Result.Failure(StripeErrors.BookingNotPayable);
        }

        return await bookingPayments.ConfirmPaymentAsync(
            booking,
            SimulatedIds.New("pi"),
            booking.TotalPrice.Amount,
            cancellationToken);
    }
}
