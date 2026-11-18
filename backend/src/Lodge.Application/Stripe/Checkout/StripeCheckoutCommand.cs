using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Payments;

namespace Lodge.Application.Stripe.Checkout;

/// <summary>
/// Represents the checkout command (Stripe Checkout, or the simulated checkout page in simulated mode).
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="BookingId">The booking identifier.</param>
public sealed record StripeCheckoutCommand(Guid UserId, Guid BookingId) : ICommand<CheckoutResponse>;
