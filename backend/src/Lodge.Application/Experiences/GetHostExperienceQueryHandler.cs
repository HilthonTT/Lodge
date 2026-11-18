using System.Data;
using Lodge.Application.Abstractions.App;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Hosting;
using Lodge.Contracts.Experiences;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Experiences;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Experiences;

internal sealed class GetHostExperienceQueryHandler(
    IHostAccess hostAccess,
    IDbConnectionFactory factory,
    IOptions<AppOptions> appOptions) : IQueryHandler<GetHostExperienceQuery, ExperienceResponse>
{
    public async Task<Result<ExperienceResponse>> Handle(GetHostExperienceQuery request, CancellationToken cancellationToken)
    {
        Result<HostCaller> caller = await hostAccess.GetCallerAsync(cancellationToken);

        if (caller.IsFailure)
        {
            return Result.Failure<ExperienceResponse>(caller.Error);
        }

        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        ExperienceResponse? experience = await ExperienceQueries.GetByIdAsync(connection, request.ExperienceId, appOptions.Value);

        if (experience is null)
        {
            return Result.Failure<ExperienceResponse>(ExperienceErrors.NotFound(request.ExperienceId));
        }

        if (!caller.Value.IsAdmin && experience.Host.Id != caller.Value.UserId)
        {
            return Result.Failure<ExperienceResponse>(ExperienceErrors.NotHost);
        }

        return experience;
    }
}
