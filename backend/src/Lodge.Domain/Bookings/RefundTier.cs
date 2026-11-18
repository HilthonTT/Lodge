namespace Lodge.Domain.Bookings;

/// <summary>
/// One step of a cancellation policy's refund timeline.
/// </summary>
/// <param name="Until">
/// The last date (inclusive) on which cancelling gives <paramref name="RefundPercent"/>; null for the last step, which
/// lasts until check-in.
/// </param>
/// <param name="RefundPercent">The refund percentage.</param>
public sealed record RefundTier(DateOnly? Until, int RefundPercent);
