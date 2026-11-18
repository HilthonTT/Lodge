using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Core.Errors;

/// <summary>
/// Contains the Stripe errors.
/// </summary>
internal static class StripeErrors
{
    internal static readonly Error NotConfigured = Error.Problem(
        "Stripe.NotConfigured",
        "Stripe is not configured. Set the Stripe:SecretKey and Stripe:WebhookSecret settings.");

    internal static readonly Error SignatureMissing = Error.Problem(
        "Stripe.SignatureMissing", "The Stripe-Signature header is missing");

    internal static readonly Error InvalidSignature = Error.Problem(
        "Stripe.InvalidSignature", "The Stripe webhook payload or signature is invalid");

    internal static readonly Error InvalidMetadata = Error.Problem(
       "Stripe.InvalidMetadata", "Invalid metadata, some fields are missing");

    internal static readonly Error BookingNotPayable = Error.Problem(
       "Stripe.BookingNotPayable", "Only reserved bookings can be paid");
}
