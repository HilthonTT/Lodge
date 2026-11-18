using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Users;

namespace Lodge.Application.Users.TwoFactor.Setup;

/// <summary>
/// Generates a new TOTP secret. Two-factor authentication is only enabled after a code is confirmed.
/// </summary>
public sealed record SetupTwoFactorCommand(Guid UserId) : ICommand<TwoFactorSetupResponse>;
