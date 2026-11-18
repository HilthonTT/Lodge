using System.ComponentModel.DataAnnotations;

namespace Lodge.Application.Stripe;

/// <summary>
/// Stripe settings. <see cref="SecretKey"/> and <see cref="WebhookSecret"/> are secrets: supply them through
/// user-secrets (<c>Stripe:SecretKey</c>) or environment variables (<c>Stripe__SecretKey</c>).
/// When they are missing the checkout/webhook endpoints answer with a "Stripe.NotConfigured" problem.
/// </summary>
public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    public string? SecretKey { get; set; }

    public string? WebhookSecret { get; set; }

    /// <summary>
    /// The URL Stripe redirects to after the checkout (success or cancel).
    /// </summary>
    [Required]
    [Url]
    public string ReturnUrl { get; set; } = "http://localhost:3000/bookings";

    public bool IsCheckoutConfigured => !string.IsNullOrWhiteSpace(SecretKey);

    public bool IsWebhookConfigured => !string.IsNullOrWhiteSpace(WebhookSecret);
}
