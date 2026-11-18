using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Authentication.ResendVerification;

/// <summary>
/// Sends a new verification e-mail to the signed-in user (at most once per minute).
/// </summary>
public sealed record ResendVerificationEmailCommand : ICommand;
