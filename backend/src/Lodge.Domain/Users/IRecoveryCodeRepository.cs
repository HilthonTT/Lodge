namespace Lodge.Domain.Users;

/// <summary>
/// Represents the <see cref="RecoveryCode"/> repository.
/// </summary>
public interface IRecoveryCodeRepository
{
    Task<List<RecoveryCode>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    void InsertRange(IEnumerable<RecoveryCode> recoveryCodes);

    void RemoveRange(IEnumerable<RecoveryCode> recoveryCodes);
}
