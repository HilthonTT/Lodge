using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Users;

namespace Lodge.Application.Users.TwoFactor.RecoveryCodes;

/// <summary>
/// Replaces the recovery codes with ten new ones. Requires a TOTP code.
/// </summary>
public sealed record RegenerateRecoveryCodesCommand(Guid UserId, string Code) : ICommand<RecoveryCodesResponse>;
