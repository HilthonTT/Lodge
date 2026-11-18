using Dapper;
using Lodge.Contracts.Users;
using System.Data;

namespace Lodge.Application.Users;

/// <summary>
/// Contains all the user queries.
/// </summary>
internal static class UserQueries
{
    /// <summary>
    /// Fetches the user by its identifier.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="userId">The user identifier.</param>
    /// <returns>An instance of <see cref="UserResponse"/> if found, otherwise null.</returns>
    public static async Task<UserResponse?> GetByIdAsync(IDbConnection connection, Guid userId)
    {
        const string sql =
            """
            SELECT
                u.id AS Id,
                u.email AS Email,
                u.first_name AS FirstName,
                u.last_name AS LastName,
                u.image_id AS ImageId,
                u.created_on_utc AS CreatedOnUtc,
                u.role AS Role
            FROM users u
            WHERE u.id = @UserId AND u.deleted = FALSE
            """;

        UserResponse? user = await connection.QueryFirstOrDefaultAsync<UserResponse>(
            sql,
            new { UserId = userId });

        return user;
    }
}
