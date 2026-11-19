using System.Data;
using Dapper;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Apartments;
using Lodge.Contracts.Common;
using Lodge.Contracts.Hosting;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Hosting.Listings;

internal sealed class GetHostListingsQueryHandler(
    IHostAccess hostAccess,
    IDbConnectionFactory factory,
    ApartmentReader apartmentReader,
    IDateTimeProvider dateTimeProvider) : IQueryHandler<GetHostListingsQuery, PagedList<HostListingSummaryResponse>>
{
    public async Task<Result<PagedList<HostListingSummaryResponse>>> Handle(
        GetHostListingsQuery request,
        CancellationToken cancellationToken)
    {
        Result<HostCaller> caller = await hostAccess.GetCallerAsync(cancellationToken);

        if (caller.IsFailure)
        {
            return Result.Failure<PagedList<HostListingSummaryResponse>>(caller.Error);
        }

        const string sql =
            """
            SELECT COUNT(*) FROM apartments AS a WHERE a.owner_id = @OwnerId AND NOT a.deleted;

            SELECT
                a.id AS Id,
                a.name AS Name,
                a.property_type AS PropertyType,
                a.address_city AS City,
                a.address_country AS Country,
                cover.id AS CoverImageId,
                cover.path AS CoverImagePath,
                a.price_amount AS Price,
                a.price_currency AS Currency,
                a.is_listed AS IsListed,
                a.instant_book AS InstantBook,
                rs.average_rating AS AverageRating,
                rs.review_count AS ReviewCount,
                bs.upcoming AS UpcomingBookingCount,
                bs.pending AS PendingRequestCount,
                a.created_on_utc AS CreatedOnUtc,
                a.modified_on_utc AS ModifiedOnUtc
            FROM apartments AS a
            LEFT JOIN LATERAL (
                SELECT i.id, i.path FROM apartment_images AS i
                WHERE i.apartment_id = a.id
                ORDER BY i.position
                LIMIT 1
            ) AS cover ON TRUE
            LEFT JOIN LATERAL (
                SELECT ROUND(AVG(r.rating)::numeric, 1) AS average_rating, COUNT(*)::int AS review_count
                FROM reviews AS r
                WHERE r.apartment_id = a.id AND r.is_published
            ) AS rs ON TRUE
            LEFT JOIN LATERAL (
                SELECT
                    COUNT(*) FILTER (WHERE b.status IN (1, 2) AND b.duration_end >= @Today::date)::int AS upcoming,
                    COUNT(*) FILTER (WHERE b.status = 6)::int AS pending
                FROM bookings AS b
                WHERE b.apartment_id = a.id
            ) AS bs ON TRUE
            WHERE a.owner_id = @OwnerId AND NOT a.deleted
            ORDER BY a.created_on_utc DESC, a.id
            LIMIT @PageSize OFFSET @Offset;
            """;

        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        using SqlMapper.GridReader grid = await connection.QueryMultipleAsync(
            sql,
            new
            {
                OwnerId = caller.Value.UserId,
                Today = DateOnly.FromDateTime(dateTimeProvider.UtcNow).ToDateTime(TimeOnly.MinValue),
                request.PageSize,
                Offset = (request.Page - 1L) * request.PageSize,
            });

        int totalCount = await grid.ReadSingleAsync<int>();

        List<HostListingSummaryResponse> items = (await grid.ReadAsync<ListingRow>())
            .Select(row => new HostListingSummaryResponse
            {
                Id = row.Id,
                Name = row.Name,
                PropertyType = ApartmentReader.PropertyTypeName(row.PropertyType),
                City = row.City,
                Country = row.Country,
                ImageUrl = row.CoverImageId is Guid imageId
                    ? apartmentReader.ImageUrl(imageId, row.CoverImagePath)
                    : string.Empty,
                Price = row.Price,
                Currency = row.Currency,
                IsListed = row.IsListed,
                InstantBook = row.InstantBook,
                AverageRating = row.AverageRating,
                ReviewCount = row.ReviewCount,
                UpcomingBookingCount = row.UpcomingBookingCount,
                PendingRequestCount = row.PendingRequestCount,
                CreatedOnUtc = row.CreatedOnUtc,
                ModifiedOnUtc = row.ModifiedOnUtc,
            })
            .ToList();

        return new PagedList<HostListingSummaryResponse>(items, request.Page, request.PageSize, totalCount);
    }

    private sealed class ListingRow
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public int PropertyType { get; init; }

        public string City { get; init; } = string.Empty;

        public string Country { get; init; } = string.Empty;

        public Guid? CoverImageId { get; init; }

        public string? CoverImagePath { get; init; }

        public decimal Price { get; init; }

        public string Currency { get; init; } = string.Empty;

        public bool IsListed { get; init; }

        public bool InstantBook { get; init; }

        public decimal? AverageRating { get; init; }

        public int ReviewCount { get; init; }

        public int UpcomingBookingCount { get; init; }

        public int PendingRequestCount { get; init; }

        public DateTime CreatedOnUtc { get; init; }

        public DateTime? ModifiedOnUtc { get; init; }
    }
}
