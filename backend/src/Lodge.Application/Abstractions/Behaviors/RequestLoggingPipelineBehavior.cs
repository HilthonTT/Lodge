using Lodge.Domain.Core.Primitives;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lodge.Application.Abstractions.Behaviors;

/// <summary>
/// Logs the start and outcome of every request.
/// </summary>
internal sealed partial class RequestLoggingPipelineBehavior<TRequest, TResponse>(
    ILogger<RequestLoggingPipelineBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        string requestName = typeof(TRequest).Name;

        LogProcessing(logger, requestName);

        TResponse result = await next(cancellationToken);

        if (result.IsSuccess)
        {
            LogCompleted(logger, requestName);
        }
        else
        {
            LogFailed(logger, requestName, result.Error.Code, result.Error.Description);
        }

        return result;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing request {RequestName}")]
    private static partial void LogProcessing(ILogger logger, string requestName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Completed request {RequestName}")]
    private static partial void LogCompleted(ILogger logger, string requestName);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Completed request {RequestName} with error {ErrorCode}: {ErrorDescription}")]
    private static partial void LogFailed(ILogger logger, string requestName, string errorCode, string errorDescription);
}
