using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Stripe.Checkout;

/// <summary>
/// Represents the stripe checkout command.
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="BookingId">The booking identifier.</param>
public sealed record StripeCheckoutCommand(Guid UserId, Guid BookingId) : ICommand<string>;
