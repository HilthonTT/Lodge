using Lodge.Application.Abstractions.Data;
using Npgsql;
using System.Data;

namespace Lodge.Persistence;

/// <summary>
/// Represents the database connection factory.
/// </summary>
internal sealed class DbConnectionFactory(NpgsqlDataSource dataSource) : IDbConnectionFactory
{
    /// <inheritdoc />
    public async Task<IDbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

        return connection;
    }
}
