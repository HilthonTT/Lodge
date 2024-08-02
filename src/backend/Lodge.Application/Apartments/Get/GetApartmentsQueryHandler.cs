using Dapper;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Apartments;
using Lodge.Contracts.Common;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using System.Data;

namespace Lodge.Application.Apartments.Get;

/// <summary>
/// Represents the <see cref="GetApartmentsQuery"/> handler.
/// </summary>
internal sealed class GetApartmentsQueryHandler(IDbConnectionFactory factory) 
    : IQueryHandler<GetApartmentsQuery, PagedList<ApartmentResponse>>
{
    private static readonly int[] ActiveBookingStatuses =
    {
        (int)BookingStatus.Reserved,
        (int)BookingStatus.Confirmed,
        (int)BookingStatus.Completed
    };

    /// <inheritdoc />
    public async Task<Result<PagedList<ApartmentResponse>>> Handle(
        GetApartmentsQuery request, 
        CancellationToken cancellationToken)
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

        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        IEnumerable<ApartmentResponse> apartmentResponses = await connection
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

        var apartments = PagedList<ApartmentResponse>.Create(
            apartmentResponses,
            request.Page,
            request.PageSize);

        return apartments;
    }
}
