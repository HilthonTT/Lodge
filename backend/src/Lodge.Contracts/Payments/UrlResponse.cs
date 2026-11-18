namespace Lodge.Contracts.Payments;

/// <summary>
/// A link (Stripe Connect onboarding or the frontend's payouts page when simulated).
/// </summary>
public sealed record UrlResponse(string Url);
