using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Authentication;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Users.Create;

/// <summary>
/// Represents the create user command.
/// </summary>
/// <param name="FirstName">The first name.</param>
/// <param name="LastName">The last name.</param>
/// <param name="Email">The email.</param>
/// <param name="Password">The password.</param>
public sealed record CreateUserCommand(
    string FirstName, 
    string LastName, 
    string Email,
    string Password) : ICommand<Result<TokenResponse>>;
