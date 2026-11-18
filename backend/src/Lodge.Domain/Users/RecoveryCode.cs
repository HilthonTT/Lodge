using Lodge.Domain.Core.Guards;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Users;

/// <summary>
/// A single-use two-factor recovery code. Only the SHA-256 hash of the (normalized) code is stored.
/// </summary>
public sealed class RecoveryCode : Entity
{
    private RecoveryCode(Guid id, Guid userId, string codeHash)
        : base(id)
    {
        UserId = userId;
        CodeHash = codeHash;
    }

    private RecoveryCode()
    {
    }

    public Guid UserId { get; private set; }

    public string CodeHash { get; private set; } = string.Empty;

    public DateTime? UsedOnUtc { get; private set; }

    public bool IsUsed => UsedOnUtc is not null;

    public static RecoveryCode Create(Guid userId, string codeHash)
    {
        Ensure.NotNullOrEmpty(codeHash);

        return new RecoveryCode(Guid.NewGuid(), userId, codeHash);
    }

    public void Use(DateTime utcNow) => UsedOnUtc ??= utcNow;
}
