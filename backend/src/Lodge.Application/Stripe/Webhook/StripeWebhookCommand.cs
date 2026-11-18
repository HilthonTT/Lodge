using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Stripe.Webhook;

/// <summary>
/// Handles a Stripe webhook call.
/// </summary>
/// <param name="Payload">The raw (unparsed) request body; required for signature verification.</param>
/// <param name="Signature">The value of the <c>Stripe-Signature</c> header.</param>
public sealed record StripeWebhookCommand(string Payload, string? Signature) : ICommand;
