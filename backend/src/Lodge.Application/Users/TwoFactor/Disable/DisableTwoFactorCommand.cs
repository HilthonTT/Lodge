using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Users.TwoFactor.Disable;

/// <summary>
/// Disables two-factor authentication. Requires the password (if the account has one) and a TOTP code.
/// </summary>
public sealed record DisableTwoFactorCommand(Guid UserId, string? Password, string Code) : ICommand;
