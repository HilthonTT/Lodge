using System.Data;
using Lodge.Application.Abstractions.App;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Hosting;
using Lodge.Contracts.Common;
using Lodge.Contracts.Experiences;
using Lodge.Domain.Core.Primitives;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Experiences;

internal sealed class GetHostExperiencesQueryHandler(
    IHostAccess hostAccess,
    IDbConnectionFactory factory,
    IDateTimeProvider dateTimeProvider,
    IOptions<AppOptions> appOptions) : IQueryHandler<GetHostExperiencesQuery, PagedList<HostExperienceSummaryResponse>>
{
    public async Task<Result<PagedList<HostExperienceSummaryResponse>>> Handle(
        GetHostExperiencesQuery request,
        CancellationToken cancellationToken)
    {
        Result<HostCaller> caller = await hostAccess.GetCallerAsync(cancellationToken);

        if (caller.IsFailure)
        {
            return Result.Failure<PagedList<HostExperienceSummaryResponse>>(caller.Error);
        }

        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        (List<HostExperienceSummaryResponse> items, int totalCount) = await ExperienceQueries.GetHostExperiencesAsync(
            connection,
            caller.Value.UserId,
            request.Page,
            request.PageSize,
            dateTimeProvider.UtcNow,
            appOptions.Value);

        return new PagedList<HostExperienceSummaryResponse>(items, request.Page, request.PageSize, totalCount);
    }
}
