using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Core.Errors;

/// <summary>
/// Contains the payment errors.
/// </summary>
internal static class PaymentErrors
{
    internal static readonly Error NotSimulated = Error.Problem(
        "Payments.NotSimulated",
        "Simulated payments are disabled: payments go through Stripe");

    internal static readonly Error PayoutAccountNotConfigured = Error.Problem(
        "Payments.PayoutsNotConfigured",
        "Stripe is not configured. Set the Stripe:SecretKey setting or use Payments:Mode = Simulated.");
}
