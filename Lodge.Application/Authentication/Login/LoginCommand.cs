using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Authentication;

namespace Lodge.Application.Authentication.Login;

/// <summary>
/// Represents the login command.
/// </summary>
/// <param name="Email">The email.</param>
/// <param name="Password">The password.</param>
public sealed record LoginCommand(string Email, string Password) : ICommand<TokenResponse>;
