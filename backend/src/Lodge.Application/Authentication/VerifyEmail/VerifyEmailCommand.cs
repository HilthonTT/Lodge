using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Authentication.VerifyEmail;

/// <summary>
/// Verifies the e-mail address with the token from the verification e-mail.
/// </summary>
public sealed record VerifyEmailCommand(string? Token) : ICommand;
