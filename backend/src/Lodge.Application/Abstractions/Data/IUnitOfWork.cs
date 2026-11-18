using Microsoft.EntityFrameworkCore.Storage;

namespace Lodge.Application.Abstractions.Data;

/// <summary>
/// Represents the unit of work.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Detaches every tracked entity so that a rolled-back unit of work is not saved later in the same scope.
    /// </summary>
    void DiscardChanges();
}
