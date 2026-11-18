using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Apartments;
using Lodge.Domain.Apartements;
using Lodge.Domain.Core.Primitives;
using System.Data;

namespace Lodge.Application.Apartments.GetById;

/// <summary>
/// Represents the <see cref="GetApartmentByIdQuery"/> handler.
/// </summary>
/// <param name="factory">The database connection factory.</param>
internal sealed class GetApartmentByIdQueryHandler(IDbConnectionFactory factory) 
    : IQueryHandler<GetApartmentByIdQuery, ApartmentResponse>
{
    /// <inheritdoc />
    public async Task<Result<ApartmentResponse>> Handle(GetApartmentByIdQuery request, CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        ApartmentResponse? apartment = await ApartmentQueries.GetByIdAsync(connection, request.ApartmentId);
        
        if (apartment is null)
        {
            return Result.Failure<ApartmentResponse>(ApartmentErrors.NotFound(request.ApartmentId));
        }

        return apartment;
    }
}
