using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Core.Errors;

/// <summary>
/// Contains the idempotency errors.
/// </summary>
internal static class IdempotencyErrors
{
    internal static readonly Error AlreadyProcessed = Error.Conflict(
        "Request.AlreadyProcessed", "Your request has already been processed");
}
