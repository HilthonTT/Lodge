using Lodge.Domain.Core.Primitives;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Lodge.Presentation.Infrastructure;

/// <summary>
/// Turns unhandled exceptions into problem details responses (without leaking exception details). Binding failures
/// (<see cref="BadHttpRequestException"/>: malformed JSON, a missing or wrongly typed route, query or header value)
/// keep their status code (usually 400) and get a stable code (<see cref="RequestErrors"/>); everything else is a 500.
/// </summary>
internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is BadHttpRequestException badRequest)
        {
            return WriteBadRequestAsync(httpContext, badRequest);
        }

        LogUnhandledException(logger, exception);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                Title = "Server failure",
                Detail = "An unexpected error occurred",
            },
        });
    }

    private ValueTask<bool> WriteBadRequestAsync(HttpContext httpContext, BadHttpRequestException exception)
    {
        Error error = RequestErrors.From(exception);
        int statusCode = exception.StatusCode is >= 400 and < 500 ? exception.StatusCode : StatusCodes.Status400BadRequest;

        LogBadRequest(logger, error.Code, exception.Message);

        httpContext.Response.StatusCode = statusCode;

        return problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Title = error.Code,
                // The framework's message names the parameter and the rejected value; it holds no server internals.
                Detail = exception.Message,
            },
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception occurred")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Rejected a request that could not be bound ({Code}): {Reason}")]
    private static partial void LogBadRequest(ILogger logger, string code, string reason);
}
