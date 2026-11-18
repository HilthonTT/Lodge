using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Hosting;
using Lodge.Contracts.Experiences;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Experiences;

namespace Lodge.Application.Experiences;

internal sealed class CreateExperienceSessionsCommandHandler(
    IHostAccess hostAccess,
    IExperienceRepository experienceRepository,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CreateExperienceSessionsCommand, CreatedSessionsResponse>
{
    public async Task<Result<CreatedSessionsResponse>> Handle(
        CreateExperienceSessionsCommand request,
        CancellationToken cancellationToken)
    {
        var managed = await HostExperiences.GetManagedAsync(hostAccess, experienceRepository, request.ExperienceId, cancellationToken);

        if (managed.IsFailure)
        {
            return Result.Failure<CreatedSessionsResponse>(managed.Error);
        }

        Experience experience = managed.Value.Experience;
        CreateSessionsRequest sessions = request.Sessions;
        DateTime utcNow = dateTimeProvider.UtcNow;

        if ((sessions.StartsAtUtc is null) == (sessions.Recurring is null))
        {
            return Result.Failure<CreatedSessionsResponse>(ExperienceSessionErrors.InvalidSchedule);
        }

        int capacity = sessions.Capacity ?? experience.MaxGuests;
        List<DateTime> starts;

        if (sessions.StartsAtUtc is DateTime startsAt)
        {
            DateTime utc = startsAt.Kind == DateTimeKind.Local ? startsAt.ToUniversalTime() : DateTime.SpecifyKind(startsAt, DateTimeKind.Utc);

            if (utc <= utcNow)
            {
                return Result.Failure<CreatedSessionsResponse>(ExperienceSessionErrors.StartsInPast);
            }

            starts = [utc];
        }
        else
        {
            Result<List<DateTime>> expanded = SessionSchedule.Expand(sessions.Recurring!);

            if (expanded.IsFailure)
            {
                return Result.Failure<CreatedSessionsResponse>(expanded.Error);
            }

            starts = expanded.Value;
        }

        HashSet<DateTime> existing = [.. await experienceRepository.GetSessionStartsAsync(experience.Id, cancellationToken)];

        List<DateTime> toCreate = [.. starts.Where(start => start > utcNow && !existing.Contains(TruncateToMinute(start))).Distinct()];

        if (toCreate.Count > SessionSchedule.MaxSessions)
        {
            return Result.Failure<CreatedSessionsResponse>(ExperienceSessionErrors.TooManySessions(SessionSchedule.MaxSessions));
        }

        if (toCreate.Count == 0)
        {
            return Result.Failure<CreatedSessionsResponse>(ExperienceSessionErrors.NoSessions);
        }

        List<Guid> ids = [];

        foreach (DateTime start in toCreate)
        {
            Result<ExperienceSession> session = ExperienceSession.Create(experience, start, capacity, utcNow);

            if (session.IsFailure)
            {
                return Result.Failure<CreatedSessionsResponse>(session.Error);
            }

            experienceRepository.InsertSession(session.Value);
            ids.Add(session.Value.Id);
        }

        return new CreatedSessionsResponse(ids);
    }

    private static DateTime TruncateToMinute(DateTime value) =>
        new(value.Ticks - (value.Ticks % TimeSpan.TicksPerMinute), DateTimeKind.Utc);
}
