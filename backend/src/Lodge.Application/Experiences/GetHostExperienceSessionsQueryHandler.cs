using System.Data;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Hosting;
using Lodge.Contracts.Experiences;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Experiences;

namespace Lodge.Application.Experiences;

internal sealed class GetHostExperienceSessionsQueryHandler(
    IHostAccess hostAccess,
    IExperienceRepository experienceRepository,
    IDbConnectionFactory factory,
    IDateTimeProvider dateTimeProvider) : IQueryHandler<GetHostExperienceSessionsQuery, List<HostExperienceSessionResponse>>
{
    public async Task<Result<List<HostExperienceSessionResponse>>> Handle(
        GetHostExperienceSessionsQuery request,
        CancellationToken cancellationToken)
    {
        var managed = await HostExperiences.GetManagedAsync(hostAccess, experienceRepository, request.ExperienceId, cancellationToken);

        if (managed.IsFailure)
        {
            return Result.Failure<List<HostExperienceSessionResponse>>(managed.Error);
        }

        (DateTime from, DateTime to) = SessionRange.Resolve(request.From, request.To, dateTimeProvider.UtcNow);

        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        return await ExperienceQueries.GetHostSessionsAsync(connection, request.ExperienceId, from, to);
    }
}
