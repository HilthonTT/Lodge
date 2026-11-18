namespace Lodge.Application.Experiences;

internal static class SessionRange
{
    public const int DefaultDays = 60;

    public const int MaxDays = 366;

    public static (DateTime From, DateTime To) Resolve(DateTime? from, DateTime? to, DateTime utcNow)
    {
        DateTime start = ToUtc(from) ?? utcNow;
        DateTime end = ToUtc(to) ?? start.AddDays(DefaultDays);

        if (end < start)
        {
            end = start;
        }

        if (end > start.AddDays(MaxDays))
        {
            end = start.AddDays(MaxDays);
        }

        return (start, end);
    }

    private static DateTime? ToUtc(DateTime? value) => value switch
    {
        null => null,
        { Kind: DateTimeKind.Local } local => local.ToUniversalTime(),
        DateTime other => DateTime.SpecifyKind(other, DateTimeKind.Utc),
    };
}
