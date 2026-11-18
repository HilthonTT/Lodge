using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Users.ChangePassword;

/// <summary>
/// Represents the change password command.
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="Password">The password.</param>
public sealed record ChangePasswordCommand(Guid UserId, string Password) : ICommand;
