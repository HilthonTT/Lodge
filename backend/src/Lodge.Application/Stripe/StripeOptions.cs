using System.ComponentModel.DataAnnotations;

namespace Lodge.Application.Stripe;

/// <summary>
/// Stripe settings. <see cref="SecretKey"/>, <see cref="WebhookSecret"/> and <see cref="ConnectWebhookSecret"/> are
/// secrets: supply them through user-secrets (<c>Stripe:SecretKey</c>) or environment variables (<c>Stripe__SecretKey</c>).
/// Without a secret key, payments are simulated in Development (see <c>Payments:Mode</c>); elsewhere the checkout
/// answers with a "Stripe.NotConfigured" problem unless <c>Payments:Mode</c> is <c>Simulated</c>. The webhook needs
/// at least one of the two webhook secrets.
/// </summary>
public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    public string? SecretKey { get; set; }

    /// <summary>
    /// The signing secret of the platform webhook endpoint (<c>checkout.session.*</c>). With the Stripe CLI, this one
    /// secret also covers forwarded Connect events.
    /// </summary>
    public string? WebhookSecret { get; set; }

    /// <summary>
    /// Optional signing secret of a separate Connect webhook endpoint (<c>account.updated</c> from connected accounts).
    /// The webhook verifies the signature against <see cref="WebhookSecret"/> first, then against this secret.
    /// </summary>
    public string? ConnectWebhookSecret { get; set; }

    /// <summary>
    /// The URL Stripe redirects to after the checkout (success or cancel).
    /// </summary>
    [Required]
    [Url]
    public string ReturnUrl { get; set; } = "http://localhost:3000/bookings";

    public bool IsCheckoutConfigured => !string.IsNullOrWhiteSpace(SecretKey);

    public bool IsWebhookConfigured => WebhookSecrets.Any();

    /// <summary>
    /// The configured webhook signing secrets, in the order they are tried.
    /// </summary>
    public IEnumerable<string> WebhookSecrets =>
        new[] { WebhookSecret, ConnectWebhookSecret }
            .Where(secret => !string.IsNullOrWhiteSpace(secret))
            .Select(secret => secret!);
}
