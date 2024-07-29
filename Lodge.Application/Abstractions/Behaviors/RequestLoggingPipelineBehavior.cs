using Lodge.Domain.Core.Primitives;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Lodge.Application.Abstractions.Behaviors;

/// <summary>
/// Represents the request logging pipeline behavior.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <param name="logger">The logger.</param>
internal sealed class RequestLoggingPipelineBehavior<TRequest, TResponse>(
    ILogger<RequestLoggingPipelineBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : Result
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        string requestName = typeof(TRequest).Name;

        logger.LogInformation("Processing request {RequestName}", requestName);

        TResponse result = await next();

        if (result.IsSuccess)
        {
            logger.LogInformation("Completed request {RequestName}", requestName);
            
            return result;
        }

        using (LogContext.PushProperty("Error", result.Error, true))
        {
            logger.LogError("Completed request {RequestName} with error", requestName);
        }

        return result;
    }
}
