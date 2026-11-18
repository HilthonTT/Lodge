namespace Lodge.Contracts.Stripe;

/// <summary>
/// Represents the stripe checkout request
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="BookingId">The booking identifier.</param>
public sealed record StripeCheckoutRequest(Guid UserId, Guid BookingId);
