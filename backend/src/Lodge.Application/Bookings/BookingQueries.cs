using System.Data;
using Dapper;
using Lodge.Contracts.Bookings;

namespace Lodge.Application.Bookings;

/// <summary>
/// Contains the Dapper read queries for bookings.
/// </summary>
internal static class BookingQueries
{
    private const string SelectBooking =
        """
        SELECT
            b.id AS Id,
            b.apartment_id AS ApartmentId,
            b.user_id AS UserId,
            b.status AS Status,
            b.price_for_period_amount AS PriceAmount,
            b.price_for_period_currency AS PriceCurrency,
            b.cleaning_fee_amount AS CleaningFeeAmount,
            b.cleaning_fee_currency AS CleaningFeeCurrency,
            b.amenities_up_charge_amount AS AmenitiesUpChargeAmount,
            b.amenities_up_charge_currency AS AmenitiesUpChargeCurrency,
            b.total_price_amount AS TotalPriceAmount,
            b.total_price_currency AS TotalPriceCurrency,
            b.duration_start AS DurationStart,
            b.duration_end AS DurationEnd,
            b.created_on_utc AS CreatedOnUtc,
            a.name AS ApartmentName,
            a.image_url AS ApartmentImageUrl,
            u.first_name || ' ' || u.last_name AS GuestName,
            u.email AS GuestEmail,
            EXISTS (SELECT 1 FROM reviews AS r WHERE r.booking_id = b.id) AS HasReview
        FROM bookings AS b
        INNER JOIN apartments AS a ON b.apartment_id = a.id
        INNER JOIN users AS u ON b.user_id = u.id
        """;

    public static Task<BookingResponse?> GetByIdAsync(IDbConnection connection, Guid bookingId) =>
        connection.QueryFirstOrDefaultAsync<BookingResponse>(
            $"""
            {SelectBooking}
            WHERE b.id = @BookingId
            """,
            new { BookingId = bookingId });

    public static async Task<List<BookingResponse>> GetByUserIdAsync(IDbConnection connection, Guid userId)
    {
        IEnumerable<BookingResponse> bookings = await connection.QueryAsync<BookingResponse>(
            $"""
            {SelectBooking}
            WHERE b.user_id = @UserId
            ORDER BY b.duration_start DESC, b.created_on_utc DESC
            """,
            new { UserId = userId });

        return bookings.ToList();
    }

    /// <summary>
    /// Returns one page of every user's bookings, newest first, optionally filtered by status.
    /// </summary>
    public static async Task<(List<BookingResponse> Items, int TotalCount)> GetPageAsync(
        IDbConnection connection,
        int? status,
        int page,
        int pageSize)
    {
        string where = status.HasValue ? "WHERE b.status = @Status" : string.Empty;

        string sql =
            $"""
            SELECT COUNT(*) FROM bookings AS b {where};

            {SelectBooking}
            {where}
            ORDER BY b.created_on_utc DESC, b.id
            LIMIT @PageSize OFFSET @Offset;
            """;

        using SqlMapper.GridReader grid = await connection.QueryMultipleAsync(
            sql,
            new { Status = status ?? 0, PageSize = pageSize, Offset = (page - 1) * pageSize });

        int totalCount = await grid.ReadSingleAsync<int>();
        List<BookingResponse> items = (await grid.ReadAsync<BookingResponse>()).ToList();

        return (items, totalCount);
    }
}
