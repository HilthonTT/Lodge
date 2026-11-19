using System.Data;
using Dapper;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Core.Extensions;
using Lodge.Application.Hosting;
using Lodge.Contracts.Common;
using Lodge.Contracts.Payments;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Payouts;

namespace Lodge.Application.Payouts;

internal sealed class GetPayoutsQueryHandler(IHostAccess hostAccess, IDbConnectionFactory factory)
    : IQueryHandler<GetPayoutsQuery, PagedList<PayoutResponse>>
{
    public async Task<Result<PagedList<PayoutResponse>>> Handle(GetPayoutsQuery request, CancellationToken cancellationToken)
    {
        Result<HostCaller> caller = await hostAccess.GetCallerAsync(cancellationToken);

        if (caller.IsFailure)
        {
            return Result.Failure<PagedList<PayoutResponse>>(caller.Error);
        }

        string? status = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : EnumNames.Parse<PayoutStatus>(request.Status).ToString();

        string where = status is null ? "p.host_id = @HostId" : "p.host_id = @HostId AND p.status = @Status";

        string sql =
            $"""
            SELECT COUNT(*) FROM payouts AS p WHERE {where};

            SELECT
                p.id AS Id,
                p.booking_id AS BookingId,
                p.experience_booking_id AS ExperienceBookingId,
                COALESCE(a.name, e.title, '') AS ListingName,
                COALESCE(u.first_name || ' ' || u.last_name, eu.first_name || ' ' || eu.last_name, '') AS GuestName,
                p.amount_amount AS Amount,
                p.amount_currency AS Currency,
                p.status AS Status,
                p.scheduled_for::timestamp AS ScheduledFor,
                p.paid_on_utc AS PaidOnUtc
            FROM payouts AS p
            LEFT JOIN bookings AS b ON b.id = p.booking_id
            LEFT JOIN apartments AS a ON a.id = b.apartment_id
            LEFT JOIN users AS u ON u.id = b.user_id
            LEFT JOIN experience_bookings AS eb ON eb.id = p.experience_booking_id
            LEFT JOIN experiences AS e ON e.id = eb.experience_id
            LEFT JOIN users AS eu ON eu.id = eb.guest_id
            WHERE {where}
            ORDER BY p.scheduled_for DESC, p.id
            LIMIT @PageSize OFFSET @Offset;
            """;

        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        using SqlMapper.GridReader grid = await connection.QueryMultipleAsync(
            sql,
            new
            {
                HostId = caller.Value.UserId,
                Status = status ?? string.Empty,
                request.PageSize,
                Offset = (request.Page - 1L) * request.PageSize,
            });

        int totalCount = await grid.ReadSingleAsync<int>();

        List<PayoutResponse> items =
        [
            .. (await grid.ReadAsync<PayoutRow>()).Select(row => new PayoutResponse
            {
                Id = row.Id,
                BookingId = row.BookingId,
                ExperienceBookingId = row.ExperienceBookingId,
                ListingName = row.ListingName,
                GuestName = row.GuestName,
                Amount = row.Amount,
                Currency = row.Currency,
                Status = row.Status,
                ScheduledFor = DateOnly.FromDateTime(row.ScheduledFor),
                PaidOnUtc = row.PaidOnUtc,
            }),
        ];

        return new PagedList<PayoutResponse>(items, request.Page, request.PageSize, totalCount);
    }

    private sealed class PayoutRow
    {
        public Guid Id { get; init; }

        public Guid? BookingId { get; init; }

        public Guid? ExperienceBookingId { get; init; }

        public string ListingName { get; init; } = string.Empty;

        public string GuestName { get; init; } = string.Empty;

        public decimal Amount { get; init; }

        public string Currency { get; init; } = string.Empty;

        public string Status { get; init; } = string.Empty;

        public DateTime ScheduledFor { get; init; }

        public DateTime? PaidOnUtc { get; init; }
    }
}
