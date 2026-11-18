using System.Text.Json;
using Lodge.Domain.Core.Primitives;
using Microsoft.AspNetCore.Http;

namespace Lodge.Presentation.Infrastructure;

/// <summary>
/// Contains the errors for requests that could not be bound (headers, bodies, route and query values).
/// </summary>
public static class RequestErrors
{
    public static readonly Error InvalidIdempotencyKey = Error.Problem(
        "Request.InvalidIdempotencyKey",
        $"The {IdempotencyKey.HeaderName} header is required and must be a GUID.");

    public static readonly Error InvalidBody = Error.Problem(
        "Request.InvalidBody",
        "The request body is missing or malformed.");

    public static readonly Error InvalidParameter = Error.Problem(
        "Request.InvalidParameter",
        "A route, query or header value is missing or has the wrong type.");

    /// <summary>
    /// Classifies a binding failure: body (JSON or form) problems versus route, query and header values.
    /// </summary>
    public static Error From(BadHttpRequestException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception.InnerException is JsonException or InvalidDataException ||
            exception.StatusCode is StatusCodes.Status413PayloadTooLarge or StatusCodes.Status415UnsupportedMediaType)
        {
            return InvalidBody;
        }

        string message = exception.Message;

        // "Failed to bind parameter "int guestCount" from "abc"." (route, query and header values).
        if (message.StartsWith("Failed to bind parameter", StringComparison.Ordinal))
        {
            return InvalidParameter;
        }

        bool isBody =
            message.EndsWith("from body.", StringComparison.Ordinal) ||
            message.EndsWith("from form.", StringComparison.Ordinal) ||
            message.Contains("request body", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("request content", StringComparison.OrdinalIgnoreCase) ||
            message.StartsWith("Implicit body", StringComparison.Ordinal);

        return isBody ? InvalidBody : InvalidParameter;
    }
}
