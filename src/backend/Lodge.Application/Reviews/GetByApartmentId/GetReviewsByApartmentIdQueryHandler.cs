using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Common;
using Lodge.Contracts.Reviews;
using Lodge.Domain.Core.Primitives;
using System.Data;

namespace Lodge.Application.Reviews.GetByApartmentId;

/// <summary>
/// Represents the <see cref="GetReviewsByApartmentIdQuery"/> handler.
/// </summary>
/// <param name="factory"></param>
internal sealed class GetReviewsByApartmentIdQueryHandler(
    IDbConnectionFactory factory) : IQueryHandler<GetReviewsByApartmentIdQuery, PagedList<ReviewResponse>>
{
    /// <inheritdoc />
    public async Task<Result<PagedList<ReviewResponse>>> Handle(
        GetReviewsByApartmentIdQuery request, 
        CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        List<ReviewResponse> reviewResponses = await ReviewQueries.GetByApartmentIdAsync(connection, request.ApartmentId);

        var reviews = PagedList<ReviewResponse>.Create(
            reviewResponses,
            request.Page,
            request.PageSize);

        return reviews;
    }
}
