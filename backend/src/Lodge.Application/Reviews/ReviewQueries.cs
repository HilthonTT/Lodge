using System.Data;
using Dapper;
using Lodge.Contracts.Reviews;

namespace Lodge.Application.Reviews;

/// <summary>
/// Contains the Dapper read queries for reviews.
/// </summary>
internal static class ReviewQueries
{
    public static async Task<(List<ReviewResponse> Items, int TotalCount)> GetByApartmentIdAsync(
        IDbConnection connection,
        Guid apartmentId,
        int page,
        int pageSize)
    {
        const string sql =
            """
            SELECT COUNT(*) FROM reviews WHERE apartment_id = @ApartmentId;

            SELECT
                r.id AS Id,
                r.apartment_id AS ApartmentId,
                r.user_id AS UserId,
                r.rating AS Rating,
                r.comment AS Comment,
                r.created_on_utc AS CreatedOnUtc,
                r.modified_on_utc AS ModifiedOnUtc,
                u.first_name || ' ' || LEFT(u.last_name, 1) || '.' AS AuthorName
            FROM reviews r
            INNER JOIN users u ON u.id = r.user_id
            WHERE r.apartment_id = @ApartmentId
            ORDER BY r.created_on_utc DESC, r.id
            LIMIT @PageSize OFFSET @Offset;
            """;

        using SqlMapper.GridReader grid = await connection.QueryMultipleAsync(
            sql,
            new { ApartmentId = apartmentId, PageSize = pageSize, Offset = (page - 1) * pageSize });

        int totalCount = await grid.ReadSingleAsync<int>();
        List<ReviewResponse> items = (await grid.ReadAsync<ReviewResponse>()).ToList();

        return (items, totalCount);
    }
}
