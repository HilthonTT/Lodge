using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Users;

namespace Lodge.Application.Users.TwoFactor.Enable;

/// <summary>
/// Confirms the TOTP setup with a code and enables two-factor authentication.
/// </summary>
public sealed record EnableTwoFactorCommand(Guid UserId, string Code) : ICommand<RecoveryCodesResponse>;
