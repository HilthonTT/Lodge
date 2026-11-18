using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Lodge.Presentation.Infrastructure;

/// <summary>
/// Turns unhandled exceptions into a 500 problem details response (without leaking exception details).
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

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception occurred")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception);
}
