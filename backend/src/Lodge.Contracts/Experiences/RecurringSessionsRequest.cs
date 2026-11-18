namespace Lodge.Contracts.Experiences;

/// <summary>
/// A weekly schedule: one session on each of <see cref="DaysOfWeek"/> (e.g. <c>"Saturday"</c>) at
/// <see cref="StartTime"/> (<c>HH:mm</c>, local time in <see cref="TimeZone"/>, an IANA id) from <see cref="From"/> to
/// <see cref="Until"/> (inclusive). At most 200 sessions; past times and existing sessions are skipped.
/// </summary>
public sealed record RecurringSessionsRequest
{
    public IReadOnlyList<string> DaysOfWeek { get; init; } = [];

    public string StartTime { get; init; } = string.Empty;

    public string TimeZone { get; init; } = string.Empty;

    public DateOnly From { get; init; }

    public DateOnly Until { get; init; }
}
