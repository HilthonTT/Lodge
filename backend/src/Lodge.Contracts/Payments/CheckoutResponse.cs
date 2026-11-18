namespace Lodge.Contracts.Payments;

/// <summary>
/// The URL to send the guest to: a Stripe Checkout page, or the frontend's simulated checkout page
/// (<c>{FrontendBaseUrl}/checkout/simulated?bookingId=...</c>).
/// </summary>
public sealed record CheckoutResponse(string Url);
