using Lodge.Application.Abstractions.Authentication;
using Lodge.Domain.Apartments;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Hosting;

/// <summary>
/// The caller of a hosting endpoint.
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="IsAdmin">Whether the user is an admin (admins may manage every listing).</param>
public sealed record HostCaller(Guid UserId, bool IsAdmin)
{
    /// <summary>
    /// Checks that the caller owns the listing (or is an admin).
    /// </summary>
    public Result EnsureCanManage(Apartment apartment) =>
        IsAdmin || apartment.IsOwnedBy(UserId) ? Result.Success() : Result.Failure(ApartmentErrors.NotOwner);

    /// <summary>
    /// Checks that the caller owns the listing with the specified owner (or is an admin).
    /// </summary>
    public Result EnsureCanManage(Guid ownerId) =>
        IsAdmin || ownerId == UserId ? Result.Success() : Result.Failure(ApartmentErrors.NotOwner);
}

/// <summary>
/// Resolves the caller of a hosting endpoint. Any user with a verified e-mail address can host.
/// </summary>
internal interface IHostAccess
{
    /// <summary>
    /// Returns the caller, or <see cref="UserErrors.EmailNotVerified"/> (403) when the e-mail address is not verified.
    /// </summary>
    Task<Result<HostCaller>> GetCallerAsync(CancellationToken cancellationToken);
}

internal sealed class HostAccess(IUserIdentifierProvider userIdentifierProvider, IUserRepository userRepository)
    : IHostAccess
{
    public async Task<Result<HostCaller>> GetCallerAsync(CancellationToken cancellationToken)
    {
        Guid userId = userIdentifierProvider.UserId;

        User? user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<HostCaller>(UserErrors.NotFound(userId));
        }

        if (!user.IsEmailVerified)
        {
            return Result.Failure<HostCaller>(UserErrors.EmailNotVerified);
        }

        return new HostCaller(user.Id, user.Role == UserRole.Admin);
    }
}
