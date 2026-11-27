using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Payments;

namespace Lodge.Application.Payouts.StartOnboarding;

/// <summary>
/// Starts (or resumes) the payout onboarding of the caller (<c>POST /hosting/payouts/onboarding</c>): a Stripe Connect
/// Express account link, or in simulated mode an immediately active account and the frontend's payouts page.
/// </summary>
public sealed record StartPayoutOnboardingCommand : ICommand<UrlResponse>;
