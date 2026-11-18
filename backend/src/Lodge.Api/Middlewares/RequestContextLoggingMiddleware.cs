using Microsoft.Extensions.Primitives;
using Serilog.Context;

namespace Lodge.Api.Middlewares;

/// <summary>
/// Represents the request context logging middleware.
/// </summary>
/// <param name="next">The request delegate.</param>
internal sealed class RequestContextLoggingMiddleware(RequestDelegate next)
{
    private const string CorrelationIdHeaderName = "X-Correlation-Id";

    /// <summary>
    /// Invokes the middleware, logging the correlation ID from the request headers or the trace identifier if the correlation ID is not present.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    public Task Invoke(HttpContext context)
    {
        using (LogContext.PushProperty("CorrelationId", GetCorrelationId(context)))
        {
            return next.Invoke(context);
        }
    }

    /// <summary>
    /// Retrieves the correlation ID from the request headers or falls back to the trace identifier if the correlation ID is not present.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <returns>The correlation ID if present in the headers, otherwise the trace identifier.</returns>
    private static string GetCorrelationId(HttpContext context)
    {
        context.Request.Headers.TryGetValue(
            CorrelationIdHeaderName,
            out StringValues correlationId);

        return correlationId.FirstOrDefault() ?? context.TraceIdentifier;
    }
}
