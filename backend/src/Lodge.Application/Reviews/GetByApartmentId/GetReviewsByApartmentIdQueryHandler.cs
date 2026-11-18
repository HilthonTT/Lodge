using System.Data;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Common;
using Lodge.Contracts.Reviews;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Reviews.GetByApartmentId;

internal sealed class GetReviewsByApartmentIdQueryHandler(IDbConnectionFactory factory)
    : IQueryHandler<GetReviewsByApartmentIdQuery, PagedList<ReviewResponse>>
{
    public async Task<Result<PagedList<ReviewResponse>>> Handle(
        GetReviewsByApartmentIdQuery request,
        CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        (List<ReviewResponse> items, int totalCount) = await ReviewQueries.GetByApartmentIdAsync(
            connection,
            request.ApartmentId,
            request.Page,
            request.PageSize);

        return new PagedList<ReviewResponse>(items, request.Page, request.PageSize, totalCount);
    }
}
