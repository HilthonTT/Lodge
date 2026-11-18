using System.Globalization;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Core.Extensions;
using Lodge.Application.Hosting;
using Lodge.Contracts.Experiences;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Experiences;

namespace Lodge.Application.Experiences;

/// <summary>
/// Expands a weekly schedule into UTC start times.
/// </summary>
internal static class SessionSchedule
{
    /// <summary>
    /// The maximum number of sessions created by one request.
    /// </summary>
    public const int MaxSessions = 200;

    /// <summary>
    /// The longest schedule range, in days.
    /// </summary>
    public const int MaxRangeDays = 366;

    public static Result<List<DateTime>> Expand(RecurringSessionsRequest recurring)
    {
        ArgumentNullException.ThrowIfNull(recurring);

        if (recurring.DaysOfWeek is null || recurring.DaysOfWeek.Count == 0 ||
            recurring.Until < recurring.From ||
            recurring.Until.DayNumber - recurring.From.DayNumber > MaxRangeDays ||
            !TimeOnly.TryParseExact(recurring.StartTime?.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly startTime))
        {
            return Result.Failure<List<DateTime>>(ExperienceSessionErrors.InvalidRecurrence);
        }

        HashSet<DayOfWeek> days = [];

        foreach (string day in recurring.DaysOfWeek)
        {
            if (!EnumNames.TryParse(day, out DayOfWeek dayOfWeek))
            {
                return Result.Failure<List<DateTime>>(ExperienceSessionErrors.InvalidRecurrence);
            }

            days.Add(dayOfWeek);
        }

        TimeZoneInfo timeZone;

        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(recurring.TimeZone?.Trim() ?? string.Empty);
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException or ArgumentException)
        {
            return Result.Failure<List<DateTime>>(ExperienceSessionErrors.InvalidTimeZone);
        }

        List<DateTime> starts = [];

        for (DateOnly date = recurring.From; date <= recurring.Until; date = date.AddDays(1))
        {
            if (!days.Contains(date.DayOfWeek))
            {
                continue;
            }

            DateTime local = date.ToDateTime(startTime, DateTimeKind.Unspecified);

            // A start time inside a daylight saving gap moves forward by the gap.
            while (timeZone.IsInvalidTime(local))
            {
                local = local.AddMinutes(30);
            }

            starts.Add(TimeZoneInfo.ConvertTimeToUtc(local, timeZone));
        }

        return starts;
    }
}

/// <summary>
/// Creates sessions of an experience: one (<c>startsAtUtc</c>) or a weekly schedule (<c>recurring</c>). Past start times
/// and start times that already have a session are skipped; at most 200 sessions are created at once.
/// </summary>
public sealed record CreateExperienceSessionsCommand(Guid ExperienceId, CreateSessionsRequest Sessions)
    : ICommand<CreatedSessionsResponse>;

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

/// <summary>
/// Removes a session without bookings, or cancels a session with bookings and refunds every booking in full.
/// </summary>
public sealed record DeleteExperienceSessionCommand(Guid ExperienceId, Guid SessionId) : ICommand;

internal sealed class DeleteExperienceSessionCommandHandler(
    IHostAccess hostAccess,
    IExperienceRepository experienceRepository,
    ExperiencePayments experiencePayments,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<DeleteExperienceSessionCommand>
{
    public async Task<Result> Handle(DeleteExperienceSessionCommand request, CancellationToken cancellationToken)
    {
        var managed = await HostExperiences.GetManagedAsync(hostAccess, experienceRepository, request.ExperienceId, cancellationToken);

        if (managed.IsFailure)
        {
            return managed;
        }

        ExperienceSession? session = await experienceRepository.GetSessionForUpdateAsync(request.SessionId, cancellationToken);

        if (session is null || session.ExperienceId != request.ExperienceId)
        {
            return Result.Failure(ExperienceSessionErrors.NotFound(request.SessionId));
        }

        List<ExperienceBooking> bookings = await experienceRepository.GetActiveBookingsOfSessionAsync(session.Id, cancellationToken);
        DateTime utcNow = dateTimeProvider.UtcNow;

        if (bookings.Count == 0 && !await experienceRepository.SessionHasBookingsAsync(session.Id, cancellationToken))
        {
            experienceRepository.RemoveSession(session);

            return Result.Success();
        }

        session.Cancel(utcNow);

        foreach (ExperienceBooking booking in bookings)
        {
            Result<decimal> cancelled = booking.Cancel(session, utcNow, byHost: true);

            if (cancelled.IsSuccess)
            {
                await experiencePayments.AdjustPayoutAsync(booking, cancelled.Value, cancellationToken);
            }
        }

        return Result.Success();
    }
}
