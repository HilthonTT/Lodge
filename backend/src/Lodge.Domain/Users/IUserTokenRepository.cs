namespace Lodge.Domain.Users;

/// <summary>
/// Represents the <see cref="UserToken"/> repository.
/// </summary>
public interface IUserTokenRepository
{
    /// <summary>
    /// Gets the token with the specified hash and purpose (consumed and expired tokens included).
    /// </summary>
    Task<UserToken?> GetByHashAsync(string tokenHash, UserTokenPurpose purpose, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets when the most recent token with the specified purpose was issued to the user, or null.
    /// </summary>
    Task<DateTime?> GetLastIssuedOnUtcAsync(Guid userId, UserTokenPurpose purpose, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the user's tokens with the specified purpose that are neither consumed nor expired.
    /// </summary>
    Task<List<UserToken>> GetUsableAsync(Guid userId, UserTokenPurpose purpose, DateTime utcNow, CancellationToken cancellationToken = default);

    void Insert(UserToken token);
}
