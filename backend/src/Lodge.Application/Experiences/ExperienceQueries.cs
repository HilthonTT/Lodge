using System.Data;
using Dapper;
using Lodge.Application.Abstractions.App;
using Lodge.Application.Experiences.Search;
using Lodge.Contracts.Experiences;
using Lodge.Domain.Experiences;

namespace Lodge.Application.Experiences;

/// <summary>
/// Dapper read queries of experiences, sessions, bookings and reviews.
/// </summary>
internal static class ExperienceQueries
{
    private const string RatingJoin =
        """
        LEFT JOIN LATERAL (
            SELECT COUNT(*)::int AS review_count, ROUND(AVG(r.rating)::numeric, 2) AS average_rating
            FROM experience_reviews AS r
            WHERE r.experience_id = e.id
        ) AS rt ON TRUE
        """;

    private const string ExperienceColumns =
        """
        e.id AS Id,
        e.title AS Title,
        e.description AS Description,
        e.category AS Category,
        e.city AS City,
        e.country AS Country,
        e.latitude AS Latitude,
        e.longitude AS Longitude,
        e.meeting_point AS MeetingPoint,
        e.duration_minutes AS DurationMinutes,
        e.price_per_guest AS PricePerGuest,
        e.currency AS Currency,
        e.max_guests AS MaxGuests,
        e.languages AS Languages,
        e.included AS Included,
        e.image_ids AS ImageIds,
        e.is_listed AS IsListed,
        e.created_on_utc AS CreatedOnUtc,
        e.host_id AS HostId,
        h.first_name AS HostFirstName,
        h.image_id AS HostImageId,
        h.is_superhost AS HostIsSuperhost,
        rt.review_count AS ReviewCount,
        rt.average_rating AS AverageRating
        """;

    private const string BookingSelect =
        """
        SELECT
            eb.id AS Id,
            e.id AS ExperienceId,
            e.title AS ExperienceTitle,
            e.category AS Category,
            e.image_ids[1] AS CoverImageId,
            e.city AS City,
            e.country AS Country,
            e.meeting_point AS MeetingPoint,
            e.duration_minutes AS DurationMinutes,
            eb.session_id AS SessionId,
            eb.session_starts_at_utc AS StartsAtUtc,
            e.host_id AS HostId,
            h.first_name AS HostFirstName,
            eb.guest_id AS GuestId,
            g.first_name || ' ' || g.last_name AS GuestName,
            eb.guest_count AS GuestCount,
            eb.price_total AS PriceTotal,
            eb.service_fee AS ServiceFee,
            eb.total_price AS TotalPrice,
            eb.currency AS Currency,
            eb.status AS Status,
            eb.amount_paid AS AmountPaid,
            eb.paid_on_utc AS PaidOnUtc,
            eb.refund_amount AS RefundAmount,
            eb.refund_status AS RefundStatus,
            eb.cancelled_by_host AS CancelledByHost,
            eb.cancelled_on_utc AS CancelledOnUtc,
            eb.created_on_utc AS CreatedOnUtc,
            EXISTS (SELECT 1 FROM experience_reviews AS r WHERE r.experience_booking_id = eb.id) AS HasReview
        FROM experience_bookings AS eb
        INNER JOIN experiences AS e ON e.id = eb.experience_id
        INNER JOIN users AS h ON h.id = e.host_id
        INNER JOIN users AS g ON g.id = eb.guest_id
        """;

    public static async Task<(List<ExperienceSummaryResponse> Items, int TotalCount)> SearchAsync(
        IDbConnection connection,
        SearchExperiencesQuery query,
        DateTime utcNow,
        string basePriceSql,
        AppOptions appOptions)
    {
        List<string> sessionConditions =
        [
            "s.experience_id = e.id",
            "s.cancelled_on_utc IS NULL",
            "s.starts_at_utc > @Now",
            "s.capacity - s.booked_guests >= @Guests",
        ];

        if (query.Date is not null)
        {
            sessionConditions.Add("s.starts_at_utc >= @DateFrom AND s.starts_at_utc < @DateTo");
        }

        List<string> conditions = ["e.is_listed", "NOT e.deleted"];

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            conditions.Add(
                """
                (unaccent(e.title) ILIKE unaccent(@Q)
                    OR unaccent(e.description) ILIKE unaccent(@Q)
                    OR unaccent(e.city) ILIKE unaccent(@Q)
                    OR unaccent(e.country) ILIKE unaccent(@Q))
                """);
        }

        if (!string.IsNullOrWhiteSpace(query.City))
        {
            conditions.Add("unaccent(e.city) ILIKE unaccent(@City)");
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            conditions.Add("e.category = @Category");
        }

        if (query.MinPrice is not null)
        {
            conditions.Add($"{basePriceSql} >= @MinPrice");
        }

        if (query.MaxPrice is not null)
        {
            conditions.Add($"{basePriceSql} <= @MaxPrice");
        }

        string sql =
            $"""
            WITH matches AS (
                SELECT e.id, ns.next_start
                FROM experiences AS e
                INNER JOIN LATERAL (
                    SELECT MIN(s.starts_at_utc) AS next_start
                    FROM experience_sessions AS s
                    WHERE {string.Join(" AND ", sessionConditions)}
                ) AS ns ON ns.next_start IS NOT NULL
                WHERE {string.Join(" AND ", conditions)}
            )
            SELECT COUNT(*)::int FROM matches;

            WITH matches AS (
                SELECT e.id, ns.next_start
                FROM experiences AS e
                INNER JOIN LATERAL (
                    SELECT MIN(s.starts_at_utc) AS next_start
                    FROM experience_sessions AS s
                    WHERE {string.Join(" AND ", sessionConditions)}
                ) AS ns ON ns.next_start IS NOT NULL
                WHERE {string.Join(" AND ", conditions)}
            )
            SELECT {ExperienceColumns}, m.next_start AS NextSessionStartsAtUtc
            FROM matches AS m
            INNER JOIN experiences AS e ON e.id = m.id
            INNER JOIN users AS h ON h.id = e.host_id
            {RatingJoin}
            ORDER BY m.next_start, e.id
            LIMIT @PageSize OFFSET @Offset;
            """;

        DateTime? dateFrom = query.Date?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        using SqlMapper.GridReader grid = await connection.QueryMultipleAsync(
            sql,
            new
            {
                Now = utcNow,
                Guests = query.Guests ?? 1,
                DateFrom = dateFrom ?? utcNow,
                DateTo = dateFrom?.AddDays(1) ?? utcNow,
                Q = string.IsNullOrWhiteSpace(query.Q) ? string.Empty : Core.Extensions.SqlPatterns.Contains(query.Q),
                City = string.IsNullOrWhiteSpace(query.City) ? string.Empty : Core.Extensions.SqlPatterns.Contains(query.City),
                Category = query.Category?.Trim() ?? string.Empty,
                MinPrice = query.MinPrice ?? 0,
                MaxPrice = query.MaxPrice ?? 0,
                query.PageSize,
                Offset = (query.Page - 1L) * query.PageSize,
            });

        int totalCount = await grid.ReadSingleAsync<int>();

        List<ExperienceSummaryResponse> items =
        [
            .. (await grid.ReadAsync<ExperienceRow>()).Select(row => new ExperienceSummaryResponse
            {
                Id = row.Id,
                Title = row.Title,
                Category = row.Category,
                City = row.City,
                Country = row.Country,
                Latitude = row.Latitude,
                Longitude = row.Longitude,
                DurationMinutes = row.DurationMinutes,
                PricePerGuest = row.PricePerGuest,
                Currency = row.Currency,
                ImageUrl = row.ImageIds is { Length: > 0 } images ? appOptions.FileUrl(images[0]) : null,
                Languages = row.Languages ?? [],
                AverageRating = row.ReviewCount > 0 ? row.AverageRating : null,
                ReviewCount = row.ReviewCount,
                NextSessionStartsAtUtc = row.NextSessionStartsAtUtc,
                HostId = row.HostId,
                HostFirstName = row.HostFirstName,
            }),
        ];

        return (items, totalCount);
    }

    /// <summary>
    /// Gets an experience (also deleted ones when <paramref name="includeDeleted"/> is set).
    /// </summary>
    public static async Task<ExperienceResponse?> GetByIdAsync(
        IDbConnection connection,
        Guid experienceId,
        AppOptions appOptions,
        bool includeDeleted = false)
    {
        ExperienceRow? row = await connection.QueryFirstOrDefaultAsync<ExperienceRow>(
            $"""
            SELECT {ExperienceColumns}, NULL::timestamptz AS NextSessionStartsAtUtc
            FROM experiences AS e
            INNER JOIN users AS h ON h.id = e.host_id
            {RatingJoin}
            WHERE e.id = @ExperienceId {(includeDeleted ? string.Empty : "AND NOT e.deleted")}
            """,
            new { ExperienceId = experienceId });

        return row is null ? null : ToResponse(row, appOptions);
    }

    public static async Task<List<ExperienceSessionResponse>> GetSessionsAsync(
        IDbConnection connection,
        Guid experienceId,
        DateTime fromUtc,
        DateTime toUtc)
    {
        IEnumerable<ExperienceSessionResponse> sessions = await connection.QueryAsync<ExperienceSessionResponse>(
            """
            SELECT s.id AS Id, s.starts_at_utc AS StartsAtUtc, s.capacity AS Capacity,
                   GREATEST(s.capacity - s.booked_guests, 0) AS SpotsLeft
            FROM experience_sessions AS s
            WHERE s.experience_id = @ExperienceId AND s.cancelled_on_utc IS NULL
              AND s.starts_at_utc >= @From AND s.starts_at_utc < @To
            ORDER BY s.starts_at_utc
            """,
            new { ExperienceId = experienceId, From = fromUtc, To = toUtc });

        return [.. sessions];
    }

    public static async Task<List<HostExperienceSessionResponse>> GetHostSessionsAsync(
        IDbConnection connection,
        Guid experienceId,
        DateTime fromUtc,
        DateTime toUtc)
    {
        IEnumerable<HostExperienceSessionResponse> sessions = await connection.QueryAsync<HostExperienceSessionResponse>(
            """
            SELECT s.id AS Id, s.starts_at_utc AS StartsAtUtc, s.capacity AS Capacity, s.booked_guests AS BookedGuests,
                   GREATEST(s.capacity - s.booked_guests, 0) AS SpotsLeft,
                   (SELECT COUNT(*)::int FROM experience_bookings AS eb
                    WHERE eb.session_id = s.id AND eb.status IN ('Reserved', 'Confirmed', 'Completed')) AS BookingCount,
                   s.cancelled_on_utc IS NOT NULL AS IsCancelled
            FROM experience_sessions AS s
            WHERE s.experience_id = @ExperienceId AND s.starts_at_utc >= @From AND s.starts_at_utc < @To
            ORDER BY s.starts_at_utc
            """,
            new { ExperienceId = experienceId, From = fromUtc, To = toUtc });

        return [.. sessions];
    }

    public static async Task<(List<ExperienceReviewResponse> Items, int TotalCount)> GetReviewsAsync(
        IDbConnection connection,
        Guid experienceId,
        int page,
        int pageSize,
        AppOptions appOptions)
    {
        using SqlMapper.GridReader grid = await connection.QueryMultipleAsync(
            """
            SELECT COUNT(*)::int FROM experience_reviews WHERE experience_id = @ExperienceId;

            SELECT r.id AS Id, r.rating AS Rating, r.comment AS Comment, r.created_on_utc AS CreatedOnUtc,
                   g.id AS GuestId, g.first_name AS GuestFirstName, g.image_id AS GuestImageId
            FROM experience_reviews AS r
            INNER JOIN users AS g ON g.id = r.guest_id
            WHERE r.experience_id = @ExperienceId
            ORDER BY r.created_on_utc DESC, r.id
            LIMIT @PageSize OFFSET @Offset;
            """,
            new { ExperienceId = experienceId, PageSize = pageSize, Offset = (page - 1L) * pageSize });

        int totalCount = await grid.ReadSingleAsync<int>();

        List<ExperienceReviewResponse> items =
        [
            .. (await grid.ReadAsync<ReviewRow>()).Select(row => new ExperienceReviewResponse(
                row.Id,
                row.Rating,
                row.Comment,
                row.CreatedOnUtc,
                new ExperienceReviewerResponse(
                    row.GuestId,
                    row.GuestFirstName,
                    row.GuestImageId is Guid imageId ? appOptions.FileUrl(imageId) : null))),
        ];

        return (items, totalCount);
    }

    public static async Task<(List<HostExperienceSummaryResponse> Items, int TotalCount)> GetHostExperiencesAsync(
        IDbConnection connection,
        Guid hostId,
        int page,
        int pageSize,
        DateTime utcNow,
        AppOptions appOptions)
    {
        using SqlMapper.GridReader grid = await connection.QueryMultipleAsync(
            $"""
            SELECT COUNT(*)::int FROM experiences AS e WHERE e.host_id = @HostId AND NOT e.deleted;

            SELECT e.id AS Id, e.title AS Title, e.category AS Category, e.city AS City, e.country AS Country,
                   e.price_per_guest AS PricePerGuest, e.currency AS Currency, e.is_listed AS IsListed,
                   e.image_ids AS ImageIds, e.created_on_utc AS CreatedOnUtc,
                   (SELECT COUNT(*)::int FROM experience_sessions AS s
                    WHERE s.experience_id = e.id AND s.cancelled_on_utc IS NULL AND s.starts_at_utc > @Now) AS UpcomingSessionCount,
                   rt.review_count AS ReviewCount, rt.average_rating AS AverageRating
            FROM experiences AS e
            {RatingJoin}
            WHERE e.host_id = @HostId AND NOT e.deleted
            ORDER BY e.created_on_utc DESC, e.id
            LIMIT @PageSize OFFSET @Offset;
            """,
            new { HostId = hostId, Now = utcNow, PageSize = pageSize, Offset = (page - 1L) * pageSize });

        int totalCount = await grid.ReadSingleAsync<int>();

        List<HostExperienceSummaryResponse> items =
        [
            .. (await grid.ReadAsync<HostExperienceRow>()).Select(row => new HostExperienceSummaryResponse
            {
                Id = row.Id,
                Title = row.Title,
                Category = row.Category,
                City = row.City,
                Country = row.Country,
                PricePerGuest = row.PricePerGuest,
                Currency = row.Currency,
                IsListed = row.IsListed,
                ImageUrl = row.ImageIds is { Length: > 0 } images ? appOptions.FileUrl(images[0]) : null,
                UpcomingSessionCount = row.UpcomingSessionCount,
                AverageRating = row.ReviewCount > 0 ? row.AverageRating : null,
                ReviewCount = row.ReviewCount,
                CreatedOnUtc = row.CreatedOnUtc,
            }),
        ];

        return (items, totalCount);
    }

    public static async Task<ExperienceBookingResponse?> GetBookingAsync(
        IDbConnection connection,
        Guid bookingId,
        DateTime utcNow,
        AppOptions appOptions)
    {
        BookingRow? row = await connection.QueryFirstOrDefaultAsync<BookingRow>(
            $"{BookingSelect} WHERE eb.id = @BookingId",
            new { BookingId = bookingId });

        return row?.ToResponse(utcNow, appOptions);
    }

    public static async Task<List<ExperienceBookingResponse>> GetGuestBookingsAsync(
        IDbConnection connection,
        Guid guestId,
        DateTime utcNow,
        AppOptions appOptions)
    {
        IEnumerable<BookingRow> rows = await connection.QueryAsync<BookingRow>(
            $"{BookingSelect} WHERE eb.guest_id = @GuestId ORDER BY eb.session_starts_at_utc DESC, eb.id",
            new { GuestId = guestId });

        return [.. rows.Select(row => row.ToResponse(utcNow, appOptions))];
    }

    public static async Task<(List<ExperienceBookingResponse> Items, int TotalCount)> GetHostBookingsAsync(
        IDbConnection connection,
        Guid hostId,
        Guid? experienceId,
        int page,
        int pageSize,
        DateTime utcNow,
        AppOptions appOptions)
    {
        string where = experienceId is null
            ? "e.host_id = @HostId"
            : "e.host_id = @HostId AND e.id = @ExperienceId";

        using SqlMapper.GridReader grid = await connection.QueryMultipleAsync(
            $"""
            SELECT COUNT(*)::int
            FROM experience_bookings AS eb
            INNER JOIN experiences AS e ON e.id = eb.experience_id
            WHERE {where};

            {BookingSelect}
            WHERE {where}
            ORDER BY eb.session_starts_at_utc DESC, eb.id
            LIMIT @PageSize OFFSET @Offset;
            """,
            new
            {
                HostId = hostId,
                ExperienceId = experienceId ?? Guid.Empty,
                PageSize = pageSize,
                Offset = (page - 1L) * pageSize,
            });

        int totalCount = await grid.ReadSingleAsync<int>();
        List<ExperienceBookingResponse> items = [.. (await grid.ReadAsync<BookingRow>()).Select(row => row.ToResponse(utcNow, appOptions))];

        return (items, totalCount);
    }

    private static ExperienceResponse ToResponse(ExperienceRow row, AppOptions appOptions) => new()
    {
        Id = row.Id,
        Title = row.Title,
        Description = row.Description,
        Category = row.Category,
        City = row.City,
        Country = row.Country,
        Latitude = row.Latitude,
        Longitude = row.Longitude,
        MeetingPoint = row.MeetingPoint,
        DurationMinutes = row.DurationMinutes,
        PricePerGuest = row.PricePerGuest,
        Currency = row.Currency,
        MaxGuests = row.MaxGuests,
        Languages = row.Languages ?? [],
        Included = row.Included ?? [],
        Images = ExperienceMapper.Images(row.ImageIds ?? [], appOptions),
        Host = new ExperienceHostResponse(
            row.HostId,
            row.HostFirstName,
            row.HostImageId is Guid imageId ? appOptions.FileUrl(imageId) : null,
            row.HostIsSuperhost),
        AverageRating = row.ReviewCount > 0 ? row.AverageRating : null,
        ReviewCount = row.ReviewCount,
        IsListed = row.IsListed,
        FreeCancellationHours = Experience.FreeCancellationHours,
        CreatedOnUtc = row.CreatedOnUtc,
    };

    private sealed class ExperienceRow
    {
        public Guid Id { get; init; }

        public string Title { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        public string Category { get; init; } = string.Empty;

        public string City { get; init; } = string.Empty;

        public string Country { get; init; } = string.Empty;

        public double Latitude { get; init; }

        public double Longitude { get; init; }

        public string MeetingPoint { get; init; } = string.Empty;

        public int DurationMinutes { get; init; }

        public decimal PricePerGuest { get; init; }

        public string Currency { get; init; } = string.Empty;

        public int MaxGuests { get; init; }

        public string[]? Languages { get; init; }

        public string[]? Included { get; init; }

        public Guid[]? ImageIds { get; init; }

        public bool IsListed { get; init; }

        public DateTime CreatedOnUtc { get; init; }

        public Guid HostId { get; init; }

        public string HostFirstName { get; init; } = string.Empty;

        public Guid? HostImageId { get; init; }

        public bool HostIsSuperhost { get; init; }

        public int ReviewCount { get; init; }

        public decimal? AverageRating { get; init; }

        public DateTime? NextSessionStartsAtUtc { get; init; }
    }

    private sealed class HostExperienceRow
    {
        public Guid Id { get; init; }

        public string Title { get; init; } = string.Empty;

        public string Category { get; init; } = string.Empty;

        public string City { get; init; } = string.Empty;

        public string Country { get; init; } = string.Empty;

        public decimal PricePerGuest { get; init; }

        public string Currency { get; init; } = string.Empty;

        public bool IsListed { get; init; }

        public Guid[]? ImageIds { get; init; }

        public DateTime CreatedOnUtc { get; init; }

        public int UpcomingSessionCount { get; init; }

        public int ReviewCount { get; init; }

        public decimal? AverageRating { get; init; }
    }

    private sealed class ReviewRow
    {
        public Guid Id { get; init; }

        public int Rating { get; init; }

        public string Comment { get; init; } = string.Empty;

        public DateTime CreatedOnUtc { get; init; }

        public Guid GuestId { get; init; }

        public string GuestFirstName { get; init; } = string.Empty;

        public Guid? GuestImageId { get; init; }
    }

    private sealed class BookingRow
    {
        public Guid Id { get; init; }

        public Guid ExperienceId { get; init; }

        public string ExperienceTitle { get; init; } = string.Empty;

        public string Category { get; init; } = string.Empty;

        public Guid? CoverImageId { get; init; }

        public string City { get; init; } = string.Empty;

        public string Country { get; init; } = string.Empty;

        public string MeetingPoint { get; init; } = string.Empty;

        public int DurationMinutes { get; init; }

        public Guid SessionId { get; init; }

        public DateTime StartsAtUtc { get; init; }

        public Guid HostId { get; init; }

        public string HostFirstName { get; init; } = string.Empty;

        public Guid GuestId { get; init; }

        public string GuestName { get; init; } = string.Empty;

        public int GuestCount { get; init; }

        public decimal PriceTotal { get; init; }

        public decimal ServiceFee { get; init; }

        public decimal TotalPrice { get; init; }

        public string Currency { get; init; } = string.Empty;

        public string Status { get; init; } = string.Empty;

        public decimal? AmountPaid { get; init; }

        public DateTime? PaidOnUtc { get; init; }

        public decimal? RefundAmount { get; init; }

        public string RefundStatus { get; init; } = "None";

        public bool CancelledByHost { get; init; }

        public DateTime? CancelledOnUtc { get; init; }

        public DateTime CreatedOnUtc { get; init; }

        public bool HasReview { get; init; }

        public ExperienceBookingResponse ToResponse(DateTime utcNow, AppOptions appOptions)
        {
            bool active = Status is nameof(ExperienceBookingStatus.Reserved) or nameof(ExperienceBookingStatus.Confirmed);

            return new ExperienceBookingResponse
            {
                Id = Id,
                ExperienceId = ExperienceId,
                ExperienceTitle = ExperienceTitle,
                Category = Category,
                ImageUrl = CoverImageId is Guid imageId ? appOptions.FileUrl(imageId) : null,
                City = City,
                Country = Country,
                MeetingPoint = MeetingPoint,
                DurationMinutes = DurationMinutes,
                SessionId = SessionId,
                StartsAtUtc = StartsAtUtc,
                HostId = HostId,
                HostFirstName = HostFirstName,
                GuestId = GuestId,
                GuestName = GuestName,
                GuestCount = GuestCount,
                PriceTotal = PriceTotal,
                ServiceFee = ServiceFee,
                TotalPrice = TotalPrice,
                Currency = Currency,
                Status = Status,
                AmountPaid = AmountPaid,
                PaidOnUtc = PaidOnUtc,
                RefundAmount = RefundAmount,
                RefundStatus = RefundStatus,
                CancelledByHost = CancelledByHost,
                CancelledOnUtc = CancelledOnUtc,
                FreeCancellationUntilUtc = StartsAtUtc.AddHours(-Experience.FreeCancellationHours),
                CanCancel = active && StartsAtUtc > utcNow,
                CanReview = Status == nameof(ExperienceBookingStatus.Completed) && !HasReview,
                HasReview = HasReview,
                CreatedOnUtc = CreatedOnUtc,
            };
        }
    }
}
