using Lodge.Application.Abstractions.Caching;
using Lodge.Contracts.Users;

namespace Lodge.Application.Users.GetById;

/// <summary>
/// Represents the query for getting a user by its identifier.
/// </summary>
/// <param name="UserId">The user identifier.</param>
public sealed record GetUserByIdQuery(Guid UserId) : ICachedQuery<UserResponse>
{
    public string CacheKey => CacheKeys.Users.GetById(UserId);

    public TimeSpan? Expiration => TimeSpan.FromMinutes(20);
}
