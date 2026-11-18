using System.Data;
using Dapper;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Hosting.Superhost;
using Lodge.Contracts.Hosting;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Hosting.Summary;

/// <summary>
/// Gets the hosting dashboard numbers of the current user.
/// </summary>
/// <remarks>
/// Upcoming stays: reserved or confirmed bookings that start today or later. Earnings: total price of confirmed and
/// completed bookings (this month: those checking in this calendar month, UTC). Ratings and the Superhost progress
/// cover every listing of the host, deleted ones included.
/// </remarks>
public sealed record GetHostingSummaryQuery : IQuery<HostingSummaryResponse>;

internal sealed class GetHostingSummaryQueryHandler(
    IHostAccess hostAccess,
    IDbConnectionFactory factory,
    IDateTimeProvider dateTimeProvider,
    IOptions<SuperhostOptions> superhostOptions) : IQueryHandler<GetHostingSummaryQuery, HostingSummaryResponse>
{
    private const string DefaultCurrency = "USD";

    public async Task<Result<HostingSummaryResponse>> Handle(
        GetHostingSummaryQuery request,
        CancellationToken cancellationToken)
    {
        Result<HostCaller> caller = await hostAccess.GetCallerAsync(cancellationToken);

        if (caller.IsFailure)
        {
            return Result.Failure<HostingSummaryResponse>(caller.Error);
        }

        Guid hostId = caller.Value.UserId;
        DateOnly today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        const string sql =
            """
            SELECT
                (SELECT COUNT(*) FROM apartments AS a WHERE a.owner_id = @HostId AND NOT a.deleted)::int AS ListingCount,
                COUNT(*) FILTER (WHERE b.status IN (1, 2) AND b.duration_start >= @Today::date)::int AS UpcomingStays,
                COUNT(*) FILTER (WHERE b.status = 6)::int AS PendingRequests,
                COALESCE(SUM(b.total_price_amount) FILTER (
                    WHERE b.status IN (2, 5)
                      AND b.duration_start >= @MonthStart::date
                      AND b.duration_start < @NextMonthStart::date), 0) AS EarningsThisMonth,
                COALESCE(SUM(b.total_price_amount) FILTER (WHERE b.status IN (2, 5)), 0) AS EarningsTotal,
                (SELECT a.price_currency FROM apartments AS a
                 WHERE a.owner_id = @HostId
                 ORDER BY a.created_on_utc
                 LIMIT 1) AS Currency,
                (SELECT u.is_superhost FROM users AS u WHERE u.id = @HostId) AS IsSuperhost
            FROM bookings AS b
            INNER JOIN apartments AS a ON a.id = b.apartment_id
            WHERE a.owner_id = @HostId
            """;

        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        SummaryRow row = await connection.QuerySingleAsync<SummaryRow>(
            sql,
            new
            {
                HostId = hostId,
                Today = today.ToDateTime(TimeOnly.MinValue),
                MonthStart = monthStart.ToDateTime(TimeOnly.MinValue),
                NextMonthStart = monthStart.AddMonths(1).ToDateTime(TimeOnly.MinValue),
            });

        HostPerformance performance =
            (await HostPerformanceQueries.GetAsync(connection, hostId)).GetValueOrDefault(hostId) ?? HostPerformance.None;

        SuperhostRequirements requirements = superhostOptions.Value.ToRequirements();

        decimal? averageRating = performance.AverageRating is decimal rating
            ? Math.Round(rating, 2, MidpointRounding.AwayFromZero)
            : null;

        return new HostingSummaryResponse(
            row.ListingCount,
            row.UpcomingStays,
            row.PendingRequests,
            row.EarningsThisMonth,
            row.EarningsTotal,
            row.Currency ?? DefaultCurrency,
            averageRating,
            performance.ReviewCount,
            row.IsSuperhost,
            new SuperhostProgressResponse(
                performance.CompletedStays,
                averageRating,
                performance.CancellationRate,
                performance.Meets(requirements),
                new SuperhostRequirementsResponse(
                    requirements.MinCompletedStays,
                    requirements.MinAverageRating,
                    requirements.MaxCancellationRate)));
    }

    private sealed class SummaryRow
    {
        public int ListingCount { get; init; }

        public int UpcomingStays { get; init; }

        public int PendingRequests { get; init; }

        public decimal EarningsThisMonth { get; init; }

        public decimal EarningsTotal { get; init; }

        public string? Currency { get; init; }

        public bool IsSuperhost { get; init; }
    }
}
