using Lodge.Domain.Users;

namespace Lodge.Application.Abstractions.Authentication;

/// <summary>
/// Represents the JWT provider interface.
/// </summary>
public interface IJwtProvider
{
    /// <summary>
    /// Creates the access token (JWT) for the specified user.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>The JWT for the specified user.</returns>
    string Create(User user);

    /// <summary>
    /// Creates a short-lived (5 minute), signed token that proves the user passed the first factor. It has its own
    /// audience, so it is not accepted as an access token.
    /// </summary>
    string CreateTwoFactorChallenge(User user);

    /// <summary>
    /// Validates a token created by <see cref="CreateTwoFactorChallenge"/>. Returns null when it is invalid or expired.
    /// </summary>
    Task<TwoFactorChallenge?> ValidateTwoFactorChallengeAsync(string token);
}
