using Lodge.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Persistence.Repositories;

/// <summary>
/// Represents the <see cref="UserToken"/> repository.
/// </summary>
internal sealed class UserTokenRepository(LodgeDbContext context) : IUserTokenRepository
{
    public Task<UserToken?> GetByHashAsync(
        string tokenHash,
        UserTokenPurpose purpose,
        CancellationToken cancellationToken = default) =>
        context.UserTokens.FirstOrDefaultAsync(
            token => token.TokenHash == tokenHash && token.Purpose == purpose,
            cancellationToken);

    public Task<DateTime?> GetLastIssuedOnUtcAsync(
        Guid userId,
        UserTokenPurpose purpose,
        CancellationToken cancellationToken = default) =>
        context.UserTokens
            .Where(token => token.UserId == userId && token.Purpose == purpose)
            .MaxAsync(token => (DateTime?)token.CreatedOnUtc, cancellationToken);

    public Task<List<UserToken>> GetUsableAsync(
        Guid userId,
        UserTokenPurpose purpose,
        DateTime utcNow,
        CancellationToken cancellationToken = default) =>
        context.UserTokens
            .Where(token => token.UserId == userId &&
                            token.Purpose == purpose &&
                            token.ConsumedOnUtc == null &&
                            token.ExpiresOnUtc > utcNow)
            .ToListAsync(cancellationToken);

    public void Insert(UserToken token) => context.UserTokens.Add(token);
}
