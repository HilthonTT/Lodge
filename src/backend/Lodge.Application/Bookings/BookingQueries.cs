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
                a.image_url AS ApartmentImageUrl
            FROM bookings AS b
            INNER JOIN apartments AS a ON b.apartment_id = a.id
            WHERE b.id = @BookingId
            """;

        BookingResponse? booking = await connection.QueryFirstOrDefaultAsync<BookingResponse>(
            sql,
            new { BookingId = bookingId });

        return booking;
    }

    /// <summary>
    /// Fetches the bookings by its user identifier.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="userId">The user identifier.</param>
    /// <returns>A list of <see cref="BookingResponse"/>.</returns>
    public static async Task<List<BookingResponse>> GetByUserIdAsync(IDbConnection connection, Guid userId)
    {
        const string sql =
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
                a.image_url AS ApartmentImageUrl
            FROM bookings AS b
            INNER JOIN apartments AS a ON b.apartment_id = a.id
            WHERE b.user_id = @UserId
            """;

        IEnumerable<BookingResponse> bookings = await connection.QueryAsync<BookingResponse>(
            sql,
            new { UserId = userId });

        return bookings.ToList();
    }
}
