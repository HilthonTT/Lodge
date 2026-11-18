using System.Data;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Apartments;
using Lodge.Domain.Apartments;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Apartments.GetById;

/// <summary>
/// Returns listed apartments only; unlisted and deleted ones are "not found" (hosts preview theirs through
/// the hosting endpoints).
/// </summary>
internal sealed class GetApartmentByIdQueryHandler(IDbConnectionFactory factory, ApartmentReader apartmentReader)
    : IQueryHandler<GetApartmentByIdQuery, ApartmentResponse>
{
    public async Task<Result<ApartmentResponse>> Handle(GetApartmentByIdQuery request, CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        List<ApartmentResponse> apartments = await apartmentReader.QueryAsync(
            connection,
            $"WHERE a.id = @ApartmentId AND {ApartmentReader.ListedCondition}",
            new { request.ApartmentId });

        return apartments.Count == 0
            ? Result.Failure<ApartmentResponse>(ApartmentErrors.NotFound(request.ApartmentId))
            : apartments[0];
    }
}
