namespace Lodge.Domain.Core.Primitives;

public interface IDateTimeProvider
{
    public DateTime UtcNow { get; }
}
