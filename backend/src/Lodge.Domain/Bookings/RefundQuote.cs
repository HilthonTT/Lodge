using Lodge.Domain.Apartments;

namespace Lodge.Domain.Bookings;

/// <summary>
/// The refund a guest gets when cancelling.
/// </summary>
/// <param name="Policy">The booking's cancellation policy.</param>
/// <param name="RefundPercent">The percentage of the refundable stay amount that is refunded.</param>
/// <param name="IsPaid">Whether the booking has been paid (confirmed); unpaid bookings have nothing to refund.</param>
/// <param name="RefundAmount">The amount refunded to the guest (0 for unpaid bookings).</param>
/// <param name="NonRefundableAmount">The paid amount that is not refunded (0 for unpaid bookings).</param>
/// <param name="RetainedStayAmount">
/// The part of the nights price (after the discount) and the amenity up-charge that is not refunded: the host's
/// share before the host service fee.
/// </param>
/// <param name="Timeline">The refund timeline of the policy for this check-in date.</param>
public sealed record RefundQuote(
    CancellationPolicy Policy,
    int RefundPercent,
    bool IsPaid,
    decimal RefundAmount,
    decimal NonRefundableAmount,
    decimal RetainedStayAmount,
    IReadOnlyList<RefundTier> Timeline);
