using Lodge.Domain.Core.Primitives;
using Microsoft.AspNetCore.Http;

namespace Lodge.Presentation.Infrastructure;

/// <summary>
/// Provides custom result handling for generating problem details responses based on the result status.
/// </summary>
public static class CustomResults
{
    /// <summary>
    /// Generates a problem details response for the given result if it represents a failure.
    /// </summary>
    /// <param name="result">The result object containing error details.</param>
    /// <returns>An <see cref="IResult"/> representing a problem details response.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the result indicates success.</exception>
    public static IResult Problem(Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException();
        }

        return Results.Problem(
            title: GetTitle(result.Error),
            detail: GetDetail(result.Error),
            type: GetType(result.Error.Type),
            statusCode: GetStatusCode(result.Error.Type),
            extensions: GetErrors(result));
    }

    /// <summary>
    /// Gets the title for the problem details response based on the error type.
    /// </summary>
    /// <param name="error">The error object containing the error details.</param>
    /// <returns>A string representing the title of the problem details response.</returns>
    private static string GetTitle(Error error) =>
            error.Type switch
            {
                ErrorType.Validation => error.Code,
                ErrorType.Problem => error.Code,
                ErrorType.NotFound => error.Code,
                ErrorType.Conflict => error.Code,
                ErrorType.Permission => error.Code,
                _ => "Server failure"
            };

    /// <summary>
    /// Gets the detail for the problem details response based on the error type.
    /// </summary>
    /// <param name="error">The error object containing the error details.</param>
    /// <returns>A string representing the detail of the problem details response.</returns>
    private static string GetDetail(Error error) =>
        error.Type switch
        {
            ErrorType.Validation => error.Description,
            ErrorType.Problem => error.Description,
            ErrorType.NotFound => error.Description,
            ErrorType.Conflict => error.Description,
            ErrorType.Permission => error.Description,
            _ => "An unexpected error occurred"
        };

    /// <summary>
    /// Gets the type URL for the problem details response based on the error type.
    /// </summary>
    /// <param name="errorType">The error type.</param>
    /// <returns>A string representing the type URL of the problem details response.</returns>
    private static string GetType(ErrorType errorType) =>
        errorType switch
        {
            ErrorType.Validation => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            ErrorType.Problem => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            ErrorType.NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            ErrorType.Conflict => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            ErrorType.Permission => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };

    /// <summary>
    /// Gets the status code for the problem details response based on the error type.
    /// </summary>
    /// <param name="errorType">The error type.</param>
    /// <returns>An integer representing the status code of the problem details response.</returns>
    private static int GetStatusCode(ErrorType errorType) =>
        errorType switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Permission => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

    /// <summary>
    /// Gets additional error details for validation errors.
    /// </summary>
    /// <param name="result">The result object containing validation error details.</param>
    /// <returns>A dictionary of additional error details, or null if the error is not a validation error.</returns>
    private static Dictionary<string, object?>? GetErrors(Result result)
    {
        if (result.Error is not ValidationError validationError)
        {
            return null;
        }

        return new Dictionary<string, object?>
        {
            { "errors", validationError.Errors }
        };
    }
}
