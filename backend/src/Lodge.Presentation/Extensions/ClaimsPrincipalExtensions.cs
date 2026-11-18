using System.Security.Claims;

namespace Lodge.Presentation.Extensions;

/// <summary>
/// Reads the caller on endpoints that also allow anonymous access.
/// </summary>
internal static class ClaimsPrincipalExtensions
{
    private const string UserIdClaim = "userId";

    public static Guid? GetUserIdOrNull(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirst(UserIdClaim)?.Value, out Guid userId) ? userId : null;
}
