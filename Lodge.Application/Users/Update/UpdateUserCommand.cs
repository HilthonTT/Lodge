using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Users.Update;

/// <summary>
/// Represents the update user command.
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="FirstName">The first name.</param>
/// <param name="LastName">The last name.</param>
public sealed record UpdateUserCommand(
    Guid UserId, 
    string FirstName, 
    string LastName) : ICommand;
