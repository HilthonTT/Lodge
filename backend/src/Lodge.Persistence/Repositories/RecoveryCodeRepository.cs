using Lodge.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Persistence.Repositories;

/// <summary>
/// Represents the <see cref="RecoveryCode"/> repository.
/// </summary>
internal sealed class RecoveryCodeRepository(LodgeDbContext context) : IRecoveryCodeRepository
{
    public Task<List<RecoveryCode>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.RecoveryCodes.Where(code => code.UserId == userId).ToListAsync(cancellationToken);

    public void InsertRange(IEnumerable<RecoveryCode> recoveryCodes) => context.RecoveryCodes.AddRange(recoveryCodes);

    public void RemoveRange(IEnumerable<RecoveryCode> recoveryCodes) => context.RecoveryCodes.RemoveRange(recoveryCodes);
}
