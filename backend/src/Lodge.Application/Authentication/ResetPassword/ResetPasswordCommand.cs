using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Authentication.ResetPassword;

/// <summary>
/// Sets a new password with the token from the password reset e-mail.
/// </summary>
public sealed record ResetPasswordCommand(string? Token, string Password) : ICommand;
