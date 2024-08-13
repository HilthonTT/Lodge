using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Stripe.Webhook;

/// <summary>
/// Represents the stripe webhook command.
/// </summary>
public sealed record StripeWebhookCommand : ICommand;
