using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Apartments;
using Lodge.Contracts.Common;
using Lodge.Domain.Core.Primitives;
using System.Data;

namespace Lodge.Application.Apartments.Get;

/// <summary>
/// Represents the <see cref="GetApartmentsQuery"/> handler.
/// </summary>
/// <param name="factory">The database connection factory.</param>
internal sealed class GetApartmentsQueryHandler(IDbConnectionFactory factory) 
    : IQueryHandler<GetApartmentsQuery, PagedList<ApartmentResponse>>
{
    /// <inheritdoc />
    public async Task<Result<PagedList<ApartmentResponse>>> Handle(
        GetApartmentsQuery request, 
        CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        List<ApartmentResponse> apartmentResponses = await ApartmentQueries.GetAsync(connection);

        var apartments = PagedList<ApartmentResponse>.Create(
            apartmentResponses,
            request.Page,
            request.PageSize);

        return apartments;
    }
}
