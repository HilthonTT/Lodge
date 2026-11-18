using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Payments;

namespace Lodge.Application.Experiences;

/// <summary>
/// Starts the payment of a reserved experience booking: a Stripe Checkout session in Stripe mode, or the frontend's
/// simulated checkout page (<c>{FrontendBaseUrl}/checkout/simulated?experienceBookingId=…</c>).
/// </summary>
public sealed record ExperienceCheckoutCommand(Guid ExperienceBookingId) : ICommand<CheckoutResponse>;
