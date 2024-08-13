using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Apartments;
using Lodge.Contracts.Common;
using Lodge.Domain.Apartements;
using Lodge.Domain.Core.Primitives;
using System.Data;

namespace Lodge.Application.Apartments.Get;

/// <summary>
/// Represents the <see cref="GetApartmentsQuery"/> handler.
/// </summary>
internal sealed class GetApartmentsQueryHandler(IDbConnectionFactory factory, IApartmentRepository apartmentRepository) 
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
