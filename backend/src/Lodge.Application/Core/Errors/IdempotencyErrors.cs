using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Core.Errors;

/// <summary>
/// Contains the idempotency errors.
/// </summary>
internal static class IdempotencyErrors
{
    internal static readonly Error KeyReused = Error.Conflict(
        "Request.KeyReused", "The idempotency key was already used for a different request");

    internal static readonly Error InProgress = Error.Conflict(
        "Request.InProgress", "A request with the same idempotency key is still being processed");

    /// <summary>
    /// A key recorded before results were stored with it: the first result cannot be replayed.
    /// </summary>
    internal static readonly Error AlreadyProcessed = Error.Conflict(
        "Request.AlreadyProcessed", "Your request has already been processed");
}
