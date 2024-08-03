using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Reviews;
using Lodge.Domain.Core.Primitives;
using System.Data;

namespace Lodge.Application.Reviews.GetByApartmentId;

/// <summary>
/// Represents the <see cref="GetReviewsByApartmentIdQuery"/> handler.
/// </summary>
/// <param name="factory"></param>
internal sealed class GetReviewsByApartmentIdQueryHandler(
    IDbConnectionFactory factory) : IQueryHandler<GetReviewsByApartmentIdQuery, List<ReviewResponse>>
{
    /// <inheritdoc />
    public async Task<Result<List<ReviewResponse>>> Handle(
        GetReviewsByApartmentIdQuery request, 
        CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        List<ReviewResponse> reviews = await ReviewQueries.GetByApartmentIdAsync(connection, request.ApartmentId);

        return reviews;
    }
}
