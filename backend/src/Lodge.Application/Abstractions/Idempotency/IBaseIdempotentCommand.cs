namespace Lodge.Application.Abstractions.Idempotency;

/// <summary>
/// Represents the base idempotent command interface.
/// </summary>
public interface IBaseIdempotentCommand
{
    Guid RequestId { get; }
}
