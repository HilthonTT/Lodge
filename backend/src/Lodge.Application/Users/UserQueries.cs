using System.Data;
using Dapper;
using Lodge.Application.Abstractions.App;
using Lodge.Contracts.Users;

namespace Lodge.Application.Users;

/// <summary>
/// Contains all the user queries.
/// </summary>
internal static class UserQueries
{
    /// <summary>
    /// Fetches the user (own account view) by its identifier.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="appOptions">The application URLs (for the avatar URL).</param>
    /// <returns>An instance of <see cref="UserResponse"/> if found, otherwise null.</returns>
    public static async Task<UserResponse?> GetByIdAsync(IDbConnection connection, Guid userId, AppOptions appOptions)
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
                u.role AS Role,
                u.email_verified_on_utc IS NOT NULL AS EmailVerified,
                u.two_factor_enabled_on_utc IS NOT NULL AS TwoFactorEnabled,
                COALESCE(u.password_hash, '') <> '' AS HasPassword,
                u.bio AS Bio
            FROM users u
            WHERE u.id = @UserId AND u.deleted = FALSE;

            SELECT el.provider
            FROM external_logins el
            WHERE el.user_id = @UserId
            ORDER BY el.provider;
            """;

        using SqlMapper.GridReader grid = await connection.QueryMultipleAsync(sql, new { UserId = userId });

        UserRow? row = await grid.ReadFirstOrDefaultAsync<UserRow>();
        List<string> externalLogins = (await grid.ReadAsync<string>()).ToList();

        if (row is null)
        {
            return null;
        }

        return new UserResponse(
            row.Id,
            row.Email,
            row.FirstName,
            row.LastName,
            row.ImageId,
            row.CreatedOnUtc,
            row.Role,
            row.EmailVerified,
            row.TwoFactorEnabled,
            row.HasPassword,
            externalLogins,
            row.ImageId is Guid imageId ? appOptions.FileUrl(imageId) : null,
            row.Bio);
    }

    /// <summary>
    /// Fetches the public profile of a user, without the listings (see <see cref="ProfileRow"/>).
    /// </summary>
    public static Task<ProfileRow?> GetProfileRowAsync(IDbConnection connection, Guid userId)
    {
        const string sql =
            """
            SELECT
                u.id AS Id,
                u.first_name AS FirstName,
                u.last_name AS LastName,
                u.image_id AS ImageId,
                u.bio AS Bio,
                u.created_on_utc AS JoinedOnUtc,
                u.email_verified_on_utc IS NOT NULL AS EmailVerified,
                u.is_superhost AS IsSuperhost,
                (SELECT COUNT(*) FROM reviews r WHERE r.user_id = u.id AND r.is_published)::int AS ReviewsWrittenCount,
                (SELECT MIN(a.created_on_utc) FROM apartments a WHERE a.owner_id = u.id AND NOT a.deleted) AS HostingSince,
                hr.review_count AS HostReviewCount,
                hr.average_rating AS HostAverageRating,
                gr.review_count AS GuestReviewCount,
                gr.average_rating AS GuestAverageRating
            FROM users u
            LEFT JOIN LATERAL (
                SELECT COUNT(*)::int AS review_count, ROUND(AVG(r.rating)::numeric, 1) AS average_rating
                FROM reviews r
                INNER JOIN apartments a ON a.id = r.apartment_id
                WHERE a.owner_id = u.id AND r.is_published
            ) AS hr ON TRUE
            LEFT JOIN LATERAL (
                SELECT COUNT(*)::int AS review_count, ROUND(AVG(g.rating)::numeric, 1) AS average_rating
                FROM guest_reviews g
                WHERE g.guest_id = u.id AND g.is_published
            ) AS gr ON TRUE
            WHERE u.id = @UserId AND u.deleted = FALSE
            """;

        return connection.QueryFirstOrDefaultAsync<ProfileRow>(sql, new { UserId = userId });
    }

    /// <summary>
    /// Gets the latest published reviews hosts wrote about the user as a guest.
    /// </summary>
    public static async Task<List<GuestReviewRow>> GetGuestReviewsAsync(IDbConnection connection, Guid userId, int limit)
    {
        const string sql =
            """
            SELECT
                g.id AS Id,
                g.rating AS Rating,
                g.comment AS Comment,
                h.first_name AS HostFirstName,
                h.image_id AS HostImageId,
                a.name AS ListingName,
                g.created_on_utc AS CreatedOnUtc
            FROM guest_reviews g
            INNER JOIN users h ON h.id = g.host_id
            INNER JOIN apartments a ON a.id = g.apartment_id
            WHERE g.guest_id = @UserId AND g.is_published
            ORDER BY g.created_on_utc DESC, g.id
            LIMIT @Limit
            """;

        return [.. await connection.QueryAsync<GuestReviewRow>(sql, new { UserId = userId, Limit = limit })];
    }

    private sealed class UserRow
    {
        public Guid Id { get; init; }

        public string Email { get; init; } = string.Empty;

        public string FirstName { get; init; } = string.Empty;

        public string LastName { get; init; } = string.Empty;

        public Guid? ImageId { get; init; }

        public DateTime CreatedOnUtc { get; init; }

        public string Role { get; init; } = string.Empty;

        public bool EmailVerified { get; init; }

        public bool TwoFactorEnabled { get; init; }

        public bool HasPassword { get; init; }

        public string? Bio { get; init; }
    }

    internal sealed class ProfileRow
    {
        public Guid Id { get; init; }

        public string FirstName { get; init; } = string.Empty;

        public string LastName { get; init; } = string.Empty;

        public Guid? ImageId { get; init; }

        public string? Bio { get; init; }

        public DateTime JoinedOnUtc { get; init; }

        public bool EmailVerified { get; init; }

        public bool IsSuperhost { get; init; }

        public int ReviewsWrittenCount { get; init; }

        public DateTime? HostingSince { get; init; }

        public int HostReviewCount { get; init; }

        public decimal? HostAverageRating { get; init; }

        public int GuestReviewCount { get; init; }

        public decimal? GuestAverageRating { get; init; }
    }

    internal sealed class GuestReviewRow
    {
        public Guid Id { get; init; }

        public int Rating { get; init; }

        public string Comment { get; init; } = string.Empty;

        public string HostFirstName { get; init; } = string.Empty;

        public Guid? HostImageId { get; init; }

        public string ListingName { get; init; } = string.Empty;

        public DateTime CreatedOnUtc { get; init; }
    }
}
