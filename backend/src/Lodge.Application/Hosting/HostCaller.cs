using Lodge.Domain.Apartments;
using Lodge.Domain.Core.Primitives;

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
