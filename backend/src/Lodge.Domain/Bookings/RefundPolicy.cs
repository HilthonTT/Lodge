using Lodge.Domain.Apartments;

namespace Lodge.Domain.Bookings;

/// <summary>
/// Calculates refunds according to the cancellation policies (see <see cref="CancellationPolicy"/>).
/// </summary>
/// <remarks>
/// The refund percentage applies to the nights price after the discount plus the amenity up-charge; the taxes are
/// refunded proportionally. The cleaning fee is always refunded (cancellations are only possible before check-in) and
/// the guest service fee only with a 100% refund. Every amount is rounded to cents.
/// </remarks>
public static class RefundPolicy
{
    /// <summary>
    /// Gets the refund percentage when cancelling <paramref name="daysBeforeCheckIn"/> days before check-in.
    /// </summary>
    public static int RefundPercent(CancellationPolicy policy, int daysBeforeCheckIn) => policy switch
    {
        CancellationPolicy.Flexible => daysBeforeCheckIn >= 1 ? 100 : 0,
        CancellationPolicy.Strict => daysBeforeCheckIn >= 14 ? 100 : daysBeforeCheckIn >= 7 ? 50 : 0,
        _ => daysBeforeCheckIn >= 5 ? 100 : 50,
    };

    /// <summary>
    /// Gets the refund timeline of a policy for a stay starting on <paramref name="checkIn"/>.
    /// </summary>
    public static IReadOnlyList<RefundTier> Timeline(CancellationPolicy policy, DateOnly checkIn) => policy switch
    {
        CancellationPolicy.Flexible => [new RefundTier(checkIn.AddDays(-1), 100), new RefundTier(null, 0)],
        CancellationPolicy.Strict =>
        [
            new RefundTier(checkIn.AddDays(-14), 100),
            new RefundTier(checkIn.AddDays(-7), 50),
            new RefundTier(null, 0),
        ],
        _ => [new RefundTier(checkIn.AddDays(-5), 100), new RefundTier(null, 50)],
    };

    /// <summary>
    /// Quotes the refund of a guest cancellation on <paramref name="today"/>.
    /// </summary>
    public static RefundQuote Quote(Booking booking, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(booking);

        int days = booking.Duration.Start.DayNumber - today.DayNumber;

        return Quote(booking, RefundPercent(booking.CancellationPolicy, days));
    }

    /// <summary>
    /// Quotes a full refund (a cancellation by the host).
    /// </summary>
    public static RefundQuote FullRefund(Booking booking) => Quote(booking, 100);

    private static RefundQuote Quote(Booking booking, int percent)
    {
        IReadOnlyList<RefundTier> timeline = Timeline(booking.CancellationPolicy, booking.Duration.Start);

        decimal refundableStay = booking.PriceForPeriod.Amount - booking.Discount.Amount + booking.AmenitiesUpCharge.Amount;
        decimal stayRefund = PricingService.Round(refundableStay * percent / 100m);
        decimal retainedStay = refundableStay - stayRefund;

        bool isPaid = booking.Status == BookingStatus.Confirmed;

        if (!isPaid)
        {
            return new RefundQuote(booking.CancellationPolicy, percent, false, 0, 0, retainedStay, timeline);
        }

        decimal taxRefund = PricingService.Round(booking.Taxes.Amount * percent / 100m);
        decimal serviceFeeRefund = percent == 100 ? booking.ServiceFee.Amount : 0;
        decimal refund = stayRefund + taxRefund + booking.CleaningFee.Amount + serviceFeeRefund;

        decimal paid = booking.AmountPaid ?? booking.TotalPrice.Amount;
        refund = Math.Min(refund, paid);

        return new RefundQuote(
            booking.CancellationPolicy,
            percent,
            true,
            refund,
            paid - refund,
            retainedStay,
            timeline);
    }
}
