namespace Lodge.Contracts.Emails;

/// <summary>
/// Represents the booking cancelled email.
/// </summary>
/// <param name="EmailTo">The email receiver.</param>
/// <param name="Name">The name receiver.</param>
/// <param name="ListingName">The listing name, if known.</param>
/// <param name="RefundAmount">The refund (paid bookings), or null.</param>
/// <param name="Currency">The currency of <paramref name="RefundAmount"/>.</param>
/// <param name="CancelledByHost">Whether the host cancelled the booking.</param>
/// <param name="RefundIsManual">Whether the refund is handled manually (payment made before payment tracking).</param>
public sealed record BookingCancelledEmail(
    string EmailTo,
    string Name,
    string? ListingName = null,
    decimal? RefundAmount = null,
    string? Currency = null,
    bool CancelledByHost = false,
    bool RefundIsManual = false);
