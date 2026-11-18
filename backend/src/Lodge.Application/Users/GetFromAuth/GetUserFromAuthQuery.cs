using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Users;

namespace Lodge.Application.Users.GetFromAuth;

/// <summary>
/// Represents the query for fetching the user using their claims.
/// </summary>
public sealed record GetUserFromAuthQuery : IQuery<UserResponse>;
