using System.Data;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Apartments.GetDisabledDates;

internal sealed class GetApartmentDisabledDatesQueryHandler(IDbConnectionFactory factory)
    : IQueryHandler<GetApartmentDisabledDatesQuery, List<DateTime>>
{
    public async Task<Result<List<DateTime>>> Handle(
        GetApartmentDisabledDatesQuery request,
        CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        List<DateOnly> nights = await ApartmentQueries.GetUnavailableNightsAsync(connection, request.ApartmentId);

        return nights.Select(night => night.ToDateTime(TimeOnly.MinValue)).ToList();
    }
}
