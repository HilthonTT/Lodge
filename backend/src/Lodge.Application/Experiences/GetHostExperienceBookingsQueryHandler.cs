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

internal sealed class GetHostExperienceBookingsQueryHandler(
    IHostAccess hostAccess,
    IDbConnectionFactory factory,
    IDateTimeProvider dateTimeProvider,
    IOptions<AppOptions> appOptions) : IQueryHandler<GetHostExperienceBookingsQuery, PagedList<ExperienceBookingResponse>>
{
    public async Task<Result<PagedList<ExperienceBookingResponse>>> Handle(
        GetHostExperienceBookingsQuery request,
        CancellationToken cancellationToken)
    {
        Result<HostCaller> caller = await hostAccess.GetCallerAsync(cancellationToken);

        if (caller.IsFailure)
        {
            return Result.Failure<PagedList<ExperienceBookingResponse>>(caller.Error);
        }

        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        (List<ExperienceBookingResponse> items, int totalCount) = await ExperienceQueries.GetHostBookingsAsync(
            connection,
            caller.Value.UserId,
            request.ExperienceId,
            request.Page,
            request.PageSize,
            dateTimeProvider.UtcNow,
            appOptions.Value);

        return new PagedList<ExperienceBookingResponse>(items, request.Page, request.PageSize, totalCount);
    }
}
