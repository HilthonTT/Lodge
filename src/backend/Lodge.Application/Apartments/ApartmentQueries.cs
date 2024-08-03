using Dapper;
using Lodge.Contracts.Apartments;
using Lodge.Domain.Bookings;
using System.Data;

namespace Lodge.Application.Apartments;

/// <summary>
/// Contains all the apartment queries.
/// </summary>
public static class ApartmentQueries
{
    private static readonly int[] ActiveBookingStatuses =
    {
        (int)BookingStatus.Reserved,
        (int)BookingStatus.Confirmed,
        (int)BookingStatus.Completed
    };

    /// <summary>
    /// Gets the apartments.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <returns>A list of <see cref="ApartmentResponse"/>.</returns>
    public static async Task<List<ApartmentResponse>> GetAsync(IDbConnection connection)
    {
        const string sql =
            """
            SELECT
                a.id AS Id,
                a.name AS Name,
                a.description AS Description,
                a.price_amount AS Price,
                a.price_currency AS Currency,
                a.image_id AS ImageId,
                a.address_country AS Country,
                a.address_state AS State,
                a.address_zip_code AS ZipCode,
                a.address_city AS City,
                a.address_street AS Street
            FROM apartments AS a
            WHERE NOT EXISTS
            (
                SELECT 1
                FROM bookings AS b
                WHERE
                    b.status = ANY(@ActiveBookingStatuses)
            )
            """;

        IEnumerable<ApartmentResponse> apartments = await connection
            .QueryAsync<ApartmentResponse, AddressResponse, ApartmentResponse>(
                sql,
                (apartment, address) =>
                {
                    apartment.Address = address;

                    return apartment;
                },
                new
                {
                    ActiveBookingStatuses,
                },
                splitOn: "Country");

        return apartments.ToList();
    }

    /// <summary>
    /// Gets the apartment by its identifier.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="userId">The apartment identifier.</param>
    /// <returns>An instance of <see cref="ApartmentResponse"/> if found, otherwise null.</returns>
    public static async Task<ApartmentResponse?> GetByIdAsync(IDbConnection connection, Guid userId)
    {
        const string sql =
            """
            SELECT
                a.id AS Id,
                a.name AS Name,
                a.description AS Description,
                a.price_amount AS Price,
                a.price_currency AS Currency,
                a.image_id AS ImageId,
                a.address_country AS Country,
                a.address_state AS State,
                a.address_zip_code AS ZipCode,
                a.address_city AS City,
                a.address_street AS Street
            FROM apartments AS a
            WHERE a.id = @Id
            """;

        ApartmentResponse? apartment = await connection.QueryFirstOrDefaultAsync<ApartmentResponse>(
            sql,
            new { Id = userId });

        return apartment;
    }
}
