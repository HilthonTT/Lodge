using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Users;

namespace Lodge.Application.Users.GetProfile;

/// <summary>
/// Gets the public profile of a user (no e-mail, no full last name). Available anonymously.
/// </summary>
public sealed record GetUserProfileQuery(Guid UserId) : IQuery<UserProfileResponse>;
