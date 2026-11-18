namespace Lodge.Contracts.Payments;

/// <summary>
/// The URL to send the guest to: a Stripe Checkout page, or the frontend's simulated checkout page
/// (<c>{FrontendBaseUrl}/checkout/simulated?bookingId=...</c>).
/// </summary>
public sealed record CheckoutResponse(string Url);

/// <summary>
/// The payment configuration the frontend needs (<c>GET /payments/config</c>).
/// </summary>
/// <param name="Mode"><c>"Stripe"</c> or <c>"Simulated"</c>.</param>
public sealed record PaymentsConfigResponse(string Mode);

/// <summary>
/// Static exchange rates for displaying prices in other currencies (<c>GET /currencies</c>). Charges always stay in
/// the listing's currency.
/// </summary>
/// <param name="Base">The base currency (<c>USD</c>).</param>
/// <param name="Rates">Units of each currency per 1 unit of <paramref name="Base"/> (the base itself is 1).</param>
/// <param name="UpdatedOnUtc">When the rates were last updated.</param>
public sealed record CurrenciesResponse(string Base, IReadOnlyDictionary<string, decimal> Rates, DateTime UpdatedOnUtc);

/// <summary>
/// A link (Stripe Connect onboarding or the frontend's payouts page when simulated).
/// </summary>
public sealed record UrlResponse(string Url);

/// <summary>
/// The host's payout account (<c>GET /hosting/payouts/account</c>).
/// </summary>
/// <param name="Status"><c>"NotConnected"</c>, <c>"Pending"</c> or <c>"Active"</c>.</param>
/// <param name="PayoutsEnabled">Whether payouts can be sent to the account.</param>
/// <param name="Mode"><c>"Stripe"</c> or <c>"Simulated"</c>.</param>
public sealed record PayoutAccountResponse(string Status, bool PayoutsEnabled, string Mode);

/// <summary>
/// A payout to the host (<c>GET /hosting/payouts</c>).
/// </summary>
public sealed class PayoutResponse
{
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the paid stay, or null for an experience payout.
    /// </summary>
    public Guid? BookingId { get; init; }

    /// <summary>
    /// Gets the paid experience booking, or null for a stay payout.
    /// </summary>
    public Guid? ExperienceBookingId { get; init; }

    /// <summary>
    /// Gets the listing name, or the experience title.
    /// </summary>
    public string ListingName { get; init; } = string.Empty;

    public string GuestName { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public string Currency { get; init; } = string.Empty;

    /// <summary>
    /// Gets <c>Scheduled</c>, <c>OnHold</c>, <c>Paid</c>, <c>Failed</c> or <c>Cancelled</c>.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    public DateOnly ScheduledFor { get; init; }

    public DateTime? PaidOnUtc { get; init; }
}
