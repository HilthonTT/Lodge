namespace Lodge.Domain.Apartments;

/// <summary>
/// Represents the apartment repository interface. Soft-deleted apartments are never returned.
/// </summary>
public interface IApartmentRepository
{
    /// <summary>
    /// Gets the apartment with the specified identifier (listed or not).
    /// </summary>
    /// <param name="id">The apartment identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The instance that may contain the apartment with the specified identifier.</returns>
    Task<Apartment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts the specified apartment to the database.
    /// </summary>
    /// <param name="apartment">The apartment to be inserted into the database.</param>
    void Insert(Apartment apartment);
}
