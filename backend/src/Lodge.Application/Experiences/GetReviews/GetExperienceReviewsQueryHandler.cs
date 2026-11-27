using System.Data;
using Lodge.Application.Abstractions.App;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Common;
using Lodge.Contracts.Experiences;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Experiences;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Experiences.GetReviews;

internal sealed class GetExperienceReviewsQueryHandler(IDbConnectionFactory factory, IOptions<AppOptions> appOptions)
    : IQueryHandler<GetExperienceReviewsQuery, PagedList<ExperienceReviewResponse>>
{
    public async Task<Result<PagedList<ExperienceReviewResponse>>> Handle(
        GetExperienceReviewsQuery request,
        CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        if (await ExperienceQueries.GetByIdAsync(connection, request.ExperienceId, appOptions.Value) is null)
        {
            return Result.Failure<PagedList<ExperienceReviewResponse>>(ExperienceErrors.NotFound(request.ExperienceId));
        }

        (List<ExperienceReviewResponse> items, int totalCount) = await ExperienceQueries.GetReviewsAsync(
            connection,
            request.ExperienceId,
            request.Page,
            request.PageSize,
            appOptions.Value);

        return new PagedList<ExperienceReviewResponse>(items, request.Page, request.PageSize, totalCount);
    }
}
