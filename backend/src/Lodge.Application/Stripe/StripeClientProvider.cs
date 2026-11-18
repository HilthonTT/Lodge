using Microsoft.Extensions.Options;
using Stripe;

namespace Lodge.Application.Stripe;

/// <summary>
/// Lazily creates a single <see cref="IStripeClient"/> (it owns an HTTP client, so it must be reused).
/// </summary>
internal sealed class StripeClientProvider(IOptions<StripeOptions> options)
{
    private readonly Lazy<IStripeClient> _client = new(() => new StripeClient(options.Value.SecretKey));

    public bool IsConfigured => options.Value.IsCheckoutConfigured;

    public IStripeClient Client => _client.Value;
}
