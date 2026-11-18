using System.Data;
using Dapper;
using Lodge.Contracts.Reviews;

namespace Lodge.Application.Reviews;

/// <summary>
/// Contains the Dapper read queries for reviews.
/// </summary>
internal static class ReviewQueries
{
    private const string SelectReview =
        """
        SELECT
            r.id AS Id,
            r.apartment_id AS ApartmentId,
            a.name AS ListingName,
            r.booking_id AS BookingId,
            r.user_id AS UserId,
            r.rating AS Rating,
            r.cleanliness_rating AS Cleanliness,
            r.accuracy_rating AS Accuracy,
            r.check_in_rating AS CheckIn,
            r.communication_rating AS Communication,
            r.location_rating AS Location,
            r.value_rating AS Value,
            r.comment AS Comment,
            r.created_on_utc AS CreatedOnUtc,
            r.modified_on_utc AS ModifiedOnUtc,
            u.first_name || ' ' || LEFT(u.last_name, 1) || '.' AS AuthorName,
            u.image_id AS AuthorImageId,
            r.host_reply AS HostReply,
            r.host_reply_created_on_utc AS HostReplyCreatedOnUtc,
            r.host_reply_modified_on_utc AS HostReplyModifiedOnUtc,
            r.is_published AS IsPublished
        FROM reviews AS r
        INNER JOIN users AS u ON u.id = r.user_id
        INNER JOIN apartments AS a ON a.id = r.apartment_id
        """;

    /// <summary>
    /// Gets the published reviews of a listing, plus the caller's own unpublished review (double-blind), newest first.
    /// </summary>
    public static Task<(List<ReviewResponse> Items, int TotalCount)> GetByApartmentIdAsync(
        IDbConnection connection,
        Guid apartmentId,
        Guid? callerId,
        int page,
        int pageSize,
        Func<Guid?, string?> avatarUrl) =>
        GetPageAsync(
            connection,
            "r.apartment_id = @ApartmentId AND (r.is_published OR r.user_id = @CallerId)",
            new { ApartmentId = apartmentId, CallerId = callerId ?? Guid.Empty },
            page,
            pageSize,
            avatarUrl);

    /// <summary>
    /// Gets the published reviews of the host's listings (deleted ones included), optionally of one listing.
    /// </summary>
    public static Task<(List<ReviewResponse> Items, int TotalCount)> GetByHostAsync(
        IDbConnection connection,
        Guid hostId,
        Guid? listingId,
        int page,
        int pageSize,
        Func<Guid?, string?> avatarUrl) =>
        GetPageAsync(
            connection,
            listingId.HasValue
                ? "a.owner_id = @HostId AND r.is_published AND r.apartment_id = @ListingId"
                : "a.owner_id = @HostId AND r.is_published",
            new { HostId = hostId, ListingId = listingId ?? Guid.Empty },
            page,
            pageSize,
            avatarUrl);

    private static async Task<(List<ReviewResponse> Items, int TotalCount)> GetPageAsync(
        IDbConnection connection,
        string condition,
        object parameters,
        int page,
        int pageSize,
        Func<Guid?, string?> avatarUrl)
    {
        string sql =
            $"""
            SELECT COUNT(*)
            FROM reviews AS r
            INNER JOIN apartments AS a ON a.id = r.apartment_id
            WHERE {condition};

            {SelectReview}
            WHERE {condition}
            ORDER BY r.created_on_utc DESC, r.id
            LIMIT @PageSize OFFSET @Offset;
            """;

        var arguments = new DynamicParameters(parameters);
        arguments.Add("PageSize", pageSize);
        arguments.Add("Offset", (page - 1) * pageSize);

        using SqlMapper.GridReader grid = await connection.QueryMultipleAsync(sql, arguments);

        int totalCount = await grid.ReadSingleAsync<int>();
        List<ReviewResponse> items = [.. (await grid.ReadAsync<ReviewRow>()).Select(row => row.ToResponse(avatarUrl))];

        return (items, totalCount);
    }

    private sealed class ReviewRow
    {
        public Guid Id { get; init; }

        public Guid ApartmentId { get; init; }

        public string ListingName { get; init; } = string.Empty;

        public Guid BookingId { get; init; }

        public Guid UserId { get; init; }

        public int Rating { get; init; }

        public int Cleanliness { get; init; }

        public int Accuracy { get; init; }

        public int CheckIn { get; init; }

        public int Communication { get; init; }

        public int Location { get; init; }

        public int Value { get; init; }

        public string Comment { get; init; } = string.Empty;

        public DateTime CreatedOnUtc { get; init; }

        public DateTime? ModifiedOnUtc { get; init; }

        public string AuthorName { get; init; } = string.Empty;

        public Guid? AuthorImageId { get; init; }

        public string? HostReply { get; init; }

        public DateTime? HostReplyCreatedOnUtc { get; init; }

        public DateTime? HostReplyModifiedOnUtc { get; init; }

        public bool IsPublished { get; init; }

        public ReviewResponse ToResponse(Func<Guid?, string?> avatarUrl) => new()
        {
            Id = Id,
            ApartmentId = ApartmentId,
            ListingName = ListingName,
            BookingId = BookingId,
            UserId = UserId,
            Rating = Rating,
            Categories = new CategoryRatingsDto
            {
                Cleanliness = Cleanliness,
                Accuracy = Accuracy,
                CheckIn = CheckIn,
                Communication = Communication,
                Location = Location,
                Value = Value,
            },
            Comment = Comment,
            CreatedOnUtc = CreatedOnUtc,
            ModifiedOnUtc = ModifiedOnUtc,
            AuthorName = AuthorName,
            AuthorAvatarUrl = avatarUrl(AuthorImageId),
            HostReply = HostReply is null
                ? null
                : new HostReplyResponse(HostReply, HostReplyCreatedOnUtc ?? CreatedOnUtc, HostReplyModifiedOnUtc),
            IsPublished = IsPublished,
        };
    }
}
