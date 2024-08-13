using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Core.Errors;

/// <summary>
/// Contains the stripe errors.
/// </summary>
internal static class StripeErrors
{
    internal static readonly Error HttpContextMissing = Error.Conflict(
        "Stripe.HttpContextMissing", "The http context is missing");

    internal static readonly Error SignatureMissing = Error.Conflict(
        "Stripe.SignatureMissing", "The signature is missing");

    internal static readonly Error NoSessionFound = Error.Conflict(
        "Stripe.NoSessionFound", "No session has been found");

    internal static readonly Error InvalidMetadata = Error.Conflict(
       "Stripe.InvalidMetadata", "Invalid metadata, some fields are missing");
}
