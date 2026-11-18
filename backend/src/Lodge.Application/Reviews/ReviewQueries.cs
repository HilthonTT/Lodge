using Dapper;
using Lodge.Contracts.Reviews;
using System.Data;

namespace Lodge.Application.Reviews;

/// <summary>
/// Contains all the review queries.
/// </summary>
public static class ReviewQueries
{
    /// <summary>
    /// Gets the reviews by their apartment identifier.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="apartmentId">The apartment identifier.</param>
    /// <returns>A list of <see cref="ReviewResponse"/>.</returns>
    public static async Task<List<ReviewResponse>> GetByApartmentIdAsync(IDbConnection connection, Guid apartmentId)
    {
        const string sql =
            """
            SELECT
                r.id AS Id,
                r.apartment_id AS ApartmentId,
                r.user_id AS UserId,
                r.rating AS Rating,
                r.comment AS Comment,
                r.created_on_utc AS CreatedOnUtc,
                r.modified_on_utc AS ModifiedOnUtc
            FROM reviews r
            WHERE r.apartment_id = @ApartmentId
            """;

        IEnumerable<ReviewResponse> reviews = await connection.QueryAsync<ReviewResponse>(
            sql,
            new { ApartmentId = apartmentId });

        return reviews.ToList();
    }
}
