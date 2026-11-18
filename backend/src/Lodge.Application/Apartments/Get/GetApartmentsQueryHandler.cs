using System.Data;
using Dapper;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Apartments;
using Lodge.Contracts.Common;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Apartments.Get;

internal sealed class GetApartmentsQueryHandler(IDbConnectionFactory factory, ApartmentReader apartmentReader)
    : IQueryHandler<GetApartmentsQuery, PagedList<ApartmentResponse>>
{
    public async Task<Result<PagedList<ApartmentResponse>>> Handle(
        GetApartmentsQuery request,
        CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        int totalCount = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM apartments AS a WHERE {ApartmentReader.ListedCondition}");

        List<ApartmentResponse> items = await apartmentReader.QueryAsync(
            connection,
            $"""
            WHERE {ApartmentReader.ListedCondition}
            ORDER BY a.name, a.id
            LIMIT @PageSize OFFSET @Offset
            """,
            new { request.PageSize, Offset = (request.Page - 1) * request.PageSize });

        return new PagedList<ApartmentResponse>(items, request.Page, request.PageSize, totalCount);
    }
}
