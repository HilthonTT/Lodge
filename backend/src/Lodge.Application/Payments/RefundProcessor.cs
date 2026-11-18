using Lodge.Application.Abstractions.Data;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Experiences;
using Microsoft.Extensions.Logging;
using Stripe;

namespace Lodge.Application.Payments;

internal sealed partial class RefundProcessor(
    IBookingRepository bookingRepository,
    IExperienceRepository experienceRepository,
    IUnitOfWork unitOfWork,
    PaymentGateways paymentGateways,
    IDateTimeProvider dateTimeProvider,
    ILogger<RefundProcessor> logger) : IRefundProcessor
{
    public async Task ProcessAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        Booking? booking = await bookingRepository.GetByIdAsync(bookingId, cancellationToken);

        if (booking is not { RefundStatus: RefundStatus.Pending, PaymentIntentId: not null, RefundAmount: > 0 })
        {
            return;
        }

        try
        {
            string refundId = await paymentGateways.For(booking.PaymentIntentId).RefundAsync(
                booking.PaymentIntentId,
                booking.RefundAmount.Value,
                booking.TotalPrice.Currency.Code,
                booking.Id.ToString(),
                cancellationToken);

            booking.MarkRefundSucceeded(refundId, dateTimeProvider.UtcNow);
        }
        catch (StripeException exception)
        {
            LogRefundFailed(logger, exception, booking.Id);
            booking.MarkRefundFailed();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ProcessExperienceAsync(Guid experienceBookingId, CancellationToken cancellationToken = default)
    {
        ExperienceBooking? booking = await experienceRepository.GetBookingAsync(experienceBookingId, cancellationToken);

        if (booking is not { RefundStatus: RefundStatus.Pending, PaymentIntentId: not null, RefundAmount: > 0 })
        {
            return;
        }

        try
        {
            string refundId = await paymentGateways.For(booking.PaymentIntentId).RefundAsync(
                booking.PaymentIntentId,
                booking.RefundAmount.Value,
                booking.Currency,
                $"experience-{booking.Id}",
                cancellationToken);

            booking.MarkRefundSucceeded(refundId, dateTimeProvider.UtcNow);
        }
        catch (StripeException exception)
        {
            LogRefundFailed(logger, exception, booking.Id);
            booking.MarkRefundFailed();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "The refund of booking {BookingId} failed")]
    private static partial void LogRefundFailed(ILogger logger, Exception exception, Guid bookingId);
}
