using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Core.Primitives;
using System.Data;

namespace Lodge.Application.Apartments.GetDisabledDates;

/// <summary>
/// Represents the <see cref="GetApartmentDisabledDatesQuery"/> handler.
/// </summary>
/// <param name="factory">The database connection factory.</param>
internal sealed class GetApartmentDisabledDatesQueryHandler(IDbConnectionFactory factory) 
    : IQueryHandler<GetApartmentDisabledDatesQuery, List<DateTime>>
{
    /// <inheritdoc />
    public async Task<Result<List<DateTime>>> Handle(GetApartmentDisabledDatesQuery request, CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        List<DateTime> dates = await ApartmentQueries.GetBookedDatesAsync(connection, request.ApartmentId);

        return dates;
    }
}
