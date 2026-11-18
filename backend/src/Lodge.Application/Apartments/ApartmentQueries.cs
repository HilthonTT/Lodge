using System.Data;
using Dapper;
using Lodge.Contracts.Apartments;
using Lodge.Domain.Bookings;

namespace Lodge.Application.Apartments;

/// <summary>
/// Contains the Dapper read queries for apartments.
/// </summary>
internal static class ApartmentQueries
{
    /// <summary>
    /// Booking statuses that block the apartment's calendar.
    /// </summary>
    internal static readonly int[] ActiveBookingStatuses =
    [
        (int)BookingStatus.Reserved,
        (int)BookingStatus.Confirmed,
        (int)BookingStatus.Completed
    ];

    // Review statistics are aggregated once per query (not per apartment row). The address columns must stay
    // last: the multi-mapping splits on "Country".
    private const string SelectApartment =
        """
        SELECT
            a.id AS Id,
            a.name AS Name,
            a.description AS Description,
            a.price_amount AS Price,
            a.price_currency AS Currency,
            a.image_url AS ImageUrl,
            a.maximum_room_count AS MaximumRoomCount,
            a.maximum_guest_count AS MaximumGuestCount,
            rs.average_rating AS AverageRating,
            COALESCE(rs.review_count, 0) AS ReviewCount,
            a.address_country AS Country,
            a.address_state AS State,
            a.address_city AS City,
            a.address_street AS Street
        FROM apartments AS a
        LEFT JOIN (
            SELECT
                r.apartment_id,
                ROUND(AVG(r.rating)::numeric, 1) AS average_rating,
                COUNT(*)::int AS review_count
            FROM reviews AS r
            GROUP BY r.apartment_id
        ) AS rs ON rs.apartment_id = a.id
        """;

    public static async Task<(List<ApartmentResponse> Items, int TotalCount)> GetPageAsync(
        IDbConnection connection,
        int page,
        int pageSize)
    {
        const string sql =
            $"""
            SELECT COUNT(*) FROM apartments;

            {SelectApartment}
            ORDER BY a.name, a.id
            LIMIT @PageSize OFFSET @Offset;
            """;

        using SqlMapper.GridReader grid = await connection.QueryMultipleAsync(
            sql,
            new { PageSize = pageSize, Offset = (page - 1) * pageSize });

        int totalCount = await grid.ReadSingleAsync<int>();

        List<ApartmentResponse> items = grid
            .Read<ApartmentResponse, AddressResponse, ApartmentResponse>(MapAddress, splitOn: "Country")
            .ToList();

        return (items, totalCount);
    }

    public static async Task<ApartmentResponse?> GetByIdAsync(IDbConnection connection, Guid apartmentId)
    {
        const string sql =
            $"""
            {SelectApartment}
            WHERE a.id = @ApartmentId
            """;

        IEnumerable<ApartmentResponse> apartments = await connection
            .QueryAsync<ApartmentResponse, AddressResponse, ApartmentResponse>(
                sql,
                MapAddress,
                new { ApartmentId = apartmentId },
                splitOn: "Country");

        return apartments.FirstOrDefault();
    }

    /// <summary>
    /// Returns every booked night (check-in up to, but excluding, the check-out day) of active bookings.
    /// A new stay may check out on a booked night or check in on another stay's check-out day.
    /// </summary>
    public static async Task<List<DateTime>> GetBookedDatesAsync(IDbConnection connection, Guid apartmentId)
    {
        const string sql =
            """
            SELECT DISTINCT d::date AS BookedDate
            FROM bookings AS b
            CROSS JOIN LATERAL generate_series(b.duration_start, b.duration_end - 1, interval '1 day') AS d
            WHERE b.apartment_id = @ApartmentId
              AND b.status = ANY(@ActiveBookingStatuses)
            ORDER BY BookedDate
            """;

        IEnumerable<DateTime> dates = await connection.QueryAsync<DateTime>(
            sql,
            new { ApartmentId = apartmentId, ActiveBookingStatuses });

        return dates.ToList();
    }

    private static ApartmentResponse MapAddress(ApartmentResponse apartment, AddressResponse address)
    {
        apartment.Address = address;
        return apartment;
    }
}
