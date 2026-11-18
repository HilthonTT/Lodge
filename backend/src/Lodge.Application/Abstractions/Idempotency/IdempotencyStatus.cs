namespace Lodge.Application.Abstractions.Idempotency;

/// <summary>
/// The state of an idempotency key.
/// </summary>
public enum IdempotencyStatus
{
    /// <summary>The key is new: run the command.</summary>
    New,

    /// <summary>The key was used by a successful request of the same type.</summary>
    Completed,

    /// <summary>The key was used by a request of another type.</summary>
    KeyReused,

    /// <summary>Another request with the same key is still being processed.</summary>
    InProgress,
}
