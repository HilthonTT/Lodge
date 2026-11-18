using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Authentication.ForgotPassword;

/// <summary>
/// E-mails a password reset link. Always succeeds, so it cannot be used to find out which e-mails have accounts.
/// </summary>
public sealed record ForgotPasswordCommand(string Email) : ICommand;
