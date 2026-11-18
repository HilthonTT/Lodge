using Lodge.Application.Abstractions.Authentication;
using Microsoft.AspNetCore.Http;

namespace Lodge.Infrastructure.Authentication;

/// <summary>
/// Reads the authenticated user's identifier from the <c>userId</c> claim of the current request.
/// </summary>
internal sealed class UserIdentifierProvider(IHttpContextAccessor httpContextAccessor) : IUserIdentifierProvider
{
    public Guid UserId
    {
        get
        {
            string? userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst(JwtProvider.UserIdClaim)?.Value;

            return Guid.TryParse(userIdClaim, out Guid userId)
                ? userId
                : throw new InvalidOperationException("The current request is not authenticated.");
        }
    }
}
