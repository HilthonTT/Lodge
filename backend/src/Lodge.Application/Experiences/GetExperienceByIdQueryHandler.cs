using System.Data;
using Lodge.Application.Abstractions.App;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Experiences;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Experiences;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Experiences;

internal sealed class GetExperienceByIdQueryHandler(IDbConnectionFactory factory, IOptions<AppOptions> appOptions)
    : IQueryHandler<GetExperienceByIdQuery, ExperienceResponse>
{
    public async Task<Result<ExperienceResponse>> Handle(GetExperienceByIdQuery request, CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        ExperienceResponse? experience = await ExperienceQueries.GetByIdAsync(connection, request.ExperienceId, appOptions.Value);

        if (experience is null || (!experience.IsListed && experience.Host.Id != request.CallerId))
        {
            return Result.Failure<ExperienceResponse>(ExperienceErrors.NotFound(request.ExperienceId));
        }

        return experience;
    }
}
