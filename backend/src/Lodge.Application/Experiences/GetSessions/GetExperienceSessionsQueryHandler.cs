using System.Data;
using Lodge.Application.Abstractions.App;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Experiences;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Experiences;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Experiences.GetSessions;

internal sealed class GetExperienceSessionsQueryHandler(
    IDbConnectionFactory factory,
    IDateTimeProvider dateTimeProvider,
    IOptions<AppOptions> appOptions) : IQueryHandler<GetExperienceSessionsQuery, List<ExperienceSessionResponse>>
{
    public async Task<Result<List<ExperienceSessionResponse>>> Handle(
        GetExperienceSessionsQuery request,
        CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        ExperienceResponse? experience = await ExperienceQueries.GetByIdAsync(connection, request.ExperienceId, appOptions.Value);

        if (experience is not { IsListed: true })
        {
            return Result.Failure<List<ExperienceSessionResponse>>(ExperienceErrors.NotFound(request.ExperienceId));
        }

        DateTime utcNow = dateTimeProvider.UtcNow;
        (DateTime from, DateTime to) = SessionRange.Resolve(request.From, request.To, utcNow);

        // Past sessions cannot be booked.
        return await ExperienceQueries.GetSessionsAsync(connection, request.ExperienceId, from < utcNow ? utcNow : from, to);
    }
}
