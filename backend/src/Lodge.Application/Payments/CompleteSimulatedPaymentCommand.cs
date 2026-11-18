using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Payments;

/// <summary>
/// Completes the simulated checkout of the caller's reserved booking: it is confirmed exactly as the Stripe webhook
/// would (same domain path and events) with a fake payment id. Only available in simulated mode.
/// </summary>
public sealed record CompleteSimulatedPaymentCommand(Guid BookingId) : ICommand;
