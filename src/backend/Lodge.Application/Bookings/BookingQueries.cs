using Dapper;
using Lodge.Contracts.Bookings;
using System.Data;

namespace Lodge.Application.Bookings;

/// <summary>
/// Contains all the booking queries.
/// </summary>
public static class BookingQueries
{
    /// <summary>
    /// Fetches the booking by its identifier.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="bookingId">The booking identifier.</param>
    /// <returns>An instance of <see cref="BookingResponse"/> if found, otherwise null.</returns>
    public static async Task<BookingResponse?> GetByIdAsync(IDbConnection connection, Guid bookingId)
    {
        const string sql =
            """
            SELECT
                id AS Id,
                apartment_id AS ApartmentId,
                user_id AS UserId,
                status AS Status,
                price_for_period_amount AS PriceAmount,
                price_for_period_currency AS PriceCurrency,
                cleaning_fee_amount AS CleaningFeeAmount,
                cleaning_fee_currency AS CleaningFeeCurrency,
                amenities_up_charge_amount AS AmenitiesUpChargeAmount,
                amenities_up_charge_currency AS AmenitiesUpChargeCurrency,
                total_price_amount AS TotalPriceAmount,
                total_price_currency AS TotalPriceCurrency,
                duration_start AS DurationStart,
                duration_end AS DurationEnd,
                created_on_utc AS CreatedOnUtc
            FROM bookings
            WHERE id = @BookingId
            """;

        BookingResponse? booking = await connection.QueryFirstOrDefaultAsync<BookingResponse>(
            sql,
            new { BookingId = bookingId });

        return booking;
    }
}
