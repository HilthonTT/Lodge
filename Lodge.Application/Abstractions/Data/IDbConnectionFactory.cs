using System.Data;

namespace Lodge.Application.Abstractions.Data;

/// <summary>
/// Represents the database connection factory interface.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Opens a database connection.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The completed task that represents the asynchronous operation. 
    /// The task result contains the database connection interface.
    /// </returns>
    Task<IDbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken = default);
}
