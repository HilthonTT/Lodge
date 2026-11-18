using Lodge.Application.Abstractions.Data;
using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Payouts;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Application.Payments;

/// <summary>
/// The money side of the booking lifecycle: confirming a paid booking schedules the host payout, and cancelling a
/// booking adjusts it. Changes are saved by the caller (the unit of work).
/// </summary>
internal sealed class BookingPayments(
    IDbContext context,
    IPayoutRepository payoutRepository,
    BookingPricing bookingPricing,
    IDateTimeProvider dateTimeProvider)
{
    /// <summary>
    /// Records the payment, confirms the booking and schedules the host payout. Bookings that are already confirmed
    /// or completed are left unchanged (payment providers retry deliveries).
    /// </summary>
    public async Task<Result> ConfirmPaymentAsync(
        Booking booking,
        string paymentIntentId,
        decimal amountPaid,
        CancellationToken cancellationToken)
    {
        if (booking.Status is BookingStatus.Confirmed or BookingStatus.Completed)
        {
            return Result.Success();
        }

        Result confirmed = booking.ConfirmPayment(paymentIntentId, amountPaid, dateTimeProvider.UtcNow);

        if (confirmed.IsFailure)
        {
            return confirmed;
        }

        // Deleted listings keep their bookings, so the query filter is ignored.
        Guid? hostId = await context.Set<Apartment>()
            .IgnoreQueryFilters()
            .Where(apartment => apartment.Id == booking.ApartmentId)
            .Select(apartment => (Guid?)apartment.OwnerId)
            .FirstOrDefaultAsync(cancellationToken);

        if (hostId is null)
        {
            return Result.Failure(ApartmentErrors.NotFound(booking.ApartmentId));
        }

        if (await payoutRepository.GetByBookingIdAsync(booking.Id, cancellationToken) is null)
        {
            payoutRepository.Insert(Payout.Schedule(booking, hostId.Value, bookingPricing.HostServiceFeePercent));
        }

        return Result.Success();
    }

    /// <summary>
    /// Adjusts the payout of a cancelled booking: the host keeps the non-refunded part of the stay minus the host fee,
    /// or the payout is cancelled when nothing is left.
    /// </summary>
    public async Task AdjustPayoutAsync(Booking booking, RefundQuote quote, CancellationToken cancellationToken)
    {
        Payout? payout = await payoutRepository.GetByBookingIdAsync(booking.Id, cancellationToken);

        payout?.AdjustForCancellation(booking.CancelledByHost ? 0 : quote.RetainedStayAmount);
    }
}
