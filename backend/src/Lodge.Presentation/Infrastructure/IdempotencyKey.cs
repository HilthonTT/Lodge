using Lodge.Domain.Core.Primitives;
using Microsoft.AspNetCore.Http;

namespace Lodge.Presentation.Infrastructure;

/// <summary>
/// The <c>X-Idempotency-Key</c> header. Endpoints bind it as a string and parse it themselves, so that a missing or
/// malformed key is answered with <c>400 Request.InvalidIdempotencyKey</c> instead of a generic binding failure.
/// </summary>
internal static class IdempotencyKey
{
    public const string HeaderName = "X-Idempotency-Key";

    /// <summary>
    /// Parses the header value; the key must be a non-empty GUID.
    /// </summary>
    public static bool TryParse(string? value, out Guid key) =>
        Guid.TryParse(value, out key) && key != Guid.Empty;

    /// <summary>
    /// The <c>400 Request.InvalidIdempotencyKey</c> problem.
    /// </summary>
    public static IResult Invalid() => CustomResults.Problem(Result.Failure(RequestErrors.InvalidIdempotencyKey));
}
