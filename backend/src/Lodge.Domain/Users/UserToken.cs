using Lodge.Domain.Core.Guards;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Users;

/// <summary>
/// A single-use token that is e-mailed to a user (e-mail verification, password reset). Only the SHA-256 hash of
/// the token is stored, so a database leak does not reveal usable links.
/// </summary>
public sealed class UserToken : Entity
{
    private UserToken(Guid id, Guid userId, UserTokenPurpose purpose, string tokenHash, DateTime createdOnUtc, DateTime expiresOnUtc)
        : base(id)
    {
        UserId = userId;
        Purpose = purpose;
        TokenHash = tokenHash;
        CreatedOnUtc = createdOnUtc;
        ExpiresOnUtc = expiresOnUtc;
    }

    private UserToken()
    {
    }

    public Guid UserId { get; private set; }

    public UserTokenPurpose Purpose { get; private set; }

    /// <summary>
    /// Gets the lower-case hex SHA-256 hash of the token.
    /// </summary>
    public string TokenHash { get; private set; } = string.Empty;

    public DateTime CreatedOnUtc { get; private set; }

    public DateTime ExpiresOnUtc { get; private set; }

    public DateTime? ConsumedOnUtc { get; private set; }

    public static UserToken Create(Guid userId, UserTokenPurpose purpose, string tokenHash, DateTime utcNow, TimeSpan lifetime)
    {
        Ensure.NotNullOrEmpty(tokenHash);

        if (lifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, "The lifetime must be positive.");
        }

        return new UserToken(Guid.NewGuid(), userId, purpose, tokenHash, utcNow, utcNow.Add(lifetime));
    }

    public bool IsUsable(DateTime utcNow) => ConsumedOnUtc is null && utcNow < ExpiresOnUtc;

    public void Consume(DateTime utcNow) => ConsumedOnUtc ??= utcNow;
}
