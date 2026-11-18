using System.Data;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Apartments;
using Lodge.Application.Reviews;
using Lodge.Contracts.Common;
using Lodge.Contracts.Reviews;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Hosting.Reviews;

internal sealed class GetHostReviewsQueryHandler(
    IHostAccess hostAccess,
    IDbConnectionFactory factory,
    ApartmentReader apartmentReader) : IQueryHandler<GetHostReviewsQuery, PagedList<ReviewResponse>>
{
    public async Task<Result<PagedList<ReviewResponse>>> Handle(
        GetHostReviewsQuery request,
        CancellationToken cancellationToken)
    {
        Result<HostCaller> caller = await hostAccess.GetCallerAsync(cancellationToken);

        if (caller.IsFailure)
        {
            return Result.Failure<PagedList<ReviewResponse>>(caller.Error);
        }

        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        (List<ReviewResponse> items, int totalCount) = await ReviewQueries.GetByHostAsync(
            connection,
            caller.Value.UserId,
            request.ListingId,
            request.Page,
            request.PageSize,
            apartmentReader.AvatarUrl);

        return new PagedList<ReviewResponse>(items, request.Page, request.PageSize, totalCount);
    }
}
