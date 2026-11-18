using System.Globalization;
using Lodge.Application.Core.Extensions;
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
