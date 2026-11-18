using Serilog.Context;

namespace Lodge.Api.Middlewares;

/// <summary>
/// Adds a <c>CorrelationId</c> property (from the <c>X-Correlation-Id</c> header, or the trace identifier)
/// to every log event of the request and echoes it in the response.
/// </summary>
internal sealed class RequestContextLoggingMiddleware(RequestDelegate next)
{
    private const string CorrelationIdHeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = GetCorrelationId(context);

        context.Response.Headers[CorrelationIdHeaderName] = correlationId;

        // The property must stay pushed until the rest of the pipeline has completed, hence the await.
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }

    private static string GetCorrelationId(HttpContext context)
    {
        string? correlationId = context.Request.Headers[CorrelationIdHeaderName].FirstOrDefault();

        return string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > 128
            ? context.TraceIdentifier
            : correlationId;
    }
}
