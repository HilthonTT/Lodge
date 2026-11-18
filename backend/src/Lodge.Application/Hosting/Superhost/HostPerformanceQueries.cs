using System.Data;
using Dapper;
using Lodge.Domain.Bookings;
using Lodge.Domain.Users;

namespace Lodge.Application.Hosting.Superhost;

/// <summary>
/// Computes the hosts' track records (see <see cref="HostPerformance"/>) in two grouped queries.
/// </summary>
internal static class HostPerformanceQueries
{
    /// <summary>
    /// Gets the performance of every host with at least one booking or published review, or of one host.
    /// </summary>
    public static async Task<Dictionary<Guid, HostPerformance>> GetAsync(IDbConnection connection, Guid? hostId = null)
    {
        string hostFilter = hostId.HasValue ? "WHERE a.owner_id = @HostId" : string.Empty;

        string sql =
            $"""
            SELECT
                a.owner_id AS HostId,
                COUNT(*) FILTER (WHERE b.status = @Completed)::int AS CompletedStays,
                COUNT(*) FILTER (WHERE b.status = @Rejected OR (b.status = @Cancelled AND b.cancelled_by_host))::int
                    AS HostCancellations,
                COUNT(*) FILTER (
                    WHERE b.status = ANY(@Answered) OR b.accepted_on_utc IS NOT NULL OR b.cancelled_by_host)::int
                    AS TotalBookings
            FROM bookings AS b
            INNER JOIN apartments AS a ON a.id = b.apartment_id
            {hostFilter}
            GROUP BY a.owner_id;

            SELECT
                a.owner_id AS HostId,
                AVG(r.rating)::numeric AS AverageRating,
                COUNT(*)::int AS ReviewCount
            FROM reviews AS r
            INNER JOIN apartments AS a ON a.id = r.apartment_id
            WHERE r.is_published {(hostId.HasValue ? "AND a.owner_id = @HostId" : string.Empty)}
            GROUP BY a.owner_id;
            """;

        using SqlMapper.GridReader grid = await connection.QueryMultipleAsync(
            sql,
            new
            {
                HostId = hostId ?? Guid.Empty,
                Completed = (int)BookingStatus.Completed,
                Rejected = (int)BookingStatus.Rejected,
                Cancelled = (int)BookingStatus.Cancelled,
                // Bookings the host answered: accepted (still reserved, confirmed or completed) or declined/rejected.
                // Cancelled bookings count only when they had been accepted (accepted_on_utc) or the host cancelled.
                Answered = new[]
                {
                    (int)BookingStatus.Reserved,
                    (int)BookingStatus.Confirmed,
                    (int)BookingStatus.Completed,
                    (int)BookingStatus.Rejected,
                },
            });

        Dictionary<Guid, BookingStatsRow> bookings = (await grid.ReadAsync<BookingStatsRow>())
            .ToDictionary(row => row.HostId);
        Dictionary<Guid, ReviewStatsRow> reviews = (await grid.ReadAsync<ReviewStatsRow>())
            .ToDictionary(row => row.HostId);

        return bookings.Keys.Union(reviews.Keys).ToDictionary(
            id => id,
            id =>
            {
                BookingStatsRow? booking = bookings.GetValueOrDefault(id);
                ReviewStatsRow? review = reviews.GetValueOrDefault(id);

                return new HostPerformance(
                    booking?.CompletedStays ?? 0,
                    review?.AverageRating,
                    review?.ReviewCount ?? 0,
                    booking?.HostCancellations ?? 0,
                    booking?.TotalBookings ?? 0);
            });
    }

    private sealed class BookingStatsRow
    {
        public Guid HostId { get; init; }

        public int CompletedStays { get; init; }

        public int HostCancellations { get; init; }

        public int TotalBookings { get; init; }
    }

    private sealed class ReviewStatsRow
    {
        public Guid HostId { get; init; }

        public decimal? AverageRating { get; init; }

        public int ReviewCount { get; init; }
    }
}
