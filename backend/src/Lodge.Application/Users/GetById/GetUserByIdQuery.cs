using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Users;

namespace Lodge.Application.Users.GetById;

/// <summary>
/// Gets a user profile. Users may only read their own profile (it contains the e-mail address).
/// </summary>
public sealed record GetUserByIdQuery(Guid UserId) : IQuery<UserResponse>;
