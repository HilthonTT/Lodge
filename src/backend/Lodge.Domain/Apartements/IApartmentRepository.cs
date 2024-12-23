﻿namespace Lodge.Domain.Apartements;

/// <summary>
/// Represents the apartment repository interface
/// </summary>
public interface IApartmentRepository
{
    /// <summary>
    /// Gets all the apartments.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of <see cref="Apartment"/>.</returns>
    Task<List<Apartment>> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the apartment with the specified identifier.
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

    /// <summary>
    /// Removes the specified apartment from the database.
    /// </summary>
    /// <param name="apartment">The apartment to be removed from the database.</param>
    void Remove(Apartment apartment);
}
