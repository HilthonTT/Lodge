using Lodge.Domain.Core.Primitives;

namespace Lodge.Infrastructure.Time;

/// <summary>
/// Represents the date time provider service.
/// </summary>
internal sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
