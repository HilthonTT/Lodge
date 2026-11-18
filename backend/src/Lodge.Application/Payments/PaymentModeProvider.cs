using Lodge.Application.Stripe;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Payments;

internal sealed class PaymentModeProvider(
    IOptions<PaymentsOptions> paymentsOptions,
    IOptions<StripeOptions> stripeOptions,
    IHostEnvironment environment) : IPaymentModeProvider
{
    private readonly Lazy<PaymentMode> _mode = new(() => Resolve(
        paymentsOptions.Value.Mode,
        stripeOptions.Value.IsCheckoutConfigured,
        environment.IsDevelopment()));

    public PaymentMode Mode => _mode.Value;

    internal static PaymentMode Resolve(string? configuredMode, bool stripeConfigured, bool isDevelopment)
    {
        if (!string.IsNullOrWhiteSpace(configuredMode) &&
            Enum.TryParse(configuredMode.Trim(), ignoreCase: true, out PaymentMode mode))
        {
            return mode;
        }

        if (stripeConfigured)
        {
            return PaymentMode.Stripe;
        }

        // Without Stripe keys, simulated payments are the default in Development only; elsewhere they must be enabled
        // explicitly (Payments:Mode = Simulated) and Stripe answers "Stripe.NotConfigured".
        return isDevelopment ? PaymentMode.Simulated : PaymentMode.Stripe;
    }
}
