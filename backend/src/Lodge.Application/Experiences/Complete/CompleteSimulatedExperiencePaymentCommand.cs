using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Experiences.Complete;

/// <summary>
/// Completes the simulated checkout of the caller's reserved experience booking (simulated mode only), exactly as the
/// Stripe webhook would.
/// </summary>
public sealed record CompleteSimulatedExperiencePaymentCommand(Guid ExperienceBookingId) : ICommand;
