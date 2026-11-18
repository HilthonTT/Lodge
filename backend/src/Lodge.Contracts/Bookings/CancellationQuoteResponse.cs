namespace Lodge.Contracts.Bookings;

/// <summary>
/// What the guest gets back when cancelling today (<c>GET /bookings/{id}/cancellation-quote</c>).
/// </summary>
/// <param name="Policy"><c>"Flexible"</c>, <c>"Moderate"</c> or <c>"Strict"</c>.</param>
/// <param name="RefundPercent">The refund percentage of the nights (after discount) and the amenity up-charge.</param>
/// <param name="RefundAmount">The amount refunded (0 for unpaid bookings).</param>
/// <param name="NonRefundableAmount">The paid amount that is kept (0 for unpaid bookings).</param>
/// <param name="Currency">The booking currency.</param>
/// <param name="Timeline">The refund steps of the policy for this stay.</param>
/// <param name="IsPaid">Whether the booking has been paid (only confirmed bookings have something to refund).</param>
public sealed record CancellationQuoteResponse(
    string Policy,
    int RefundPercent,
    decimal RefundAmount,
    decimal NonRefundableAmount,
    string Currency,
    IReadOnlyList<RefundTimelineEntryResponse> Timeline,
    bool IsPaid);
