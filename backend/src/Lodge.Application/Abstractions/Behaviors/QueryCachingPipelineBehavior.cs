using Lodge.Application.Abstractions.Caching;
using Lodge.Domain.Core.Primitives;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lodge.Application.Abstractions.Behaviors;

/// <summary>
/// Caches successful results of <see cref="ICachedQuery"/> requests.
/// </summary>
/// <remarks>
/// The cache is consulted <em>before</em> the handler runs, so only queries whose result does not depend on
/// the caller (public data) may implement <see cref="ICachedQuery"/>; otherwise authorization checks in the
/// handler would be bypassed on a cache hit.
/// </remarks>
internal sealed partial class QueryCachingPipelineBehavior<TRequest, TResponse>(
    ICacheService cacheService,
    ILogger<QueryCachingPipelineBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICachedQuery
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        string requestName = typeof(TRequest).Name;

        TResponse? cachedResult = await cacheService.GetAsync<TResponse>(request.CacheKey, cancellationToken);

        if (cachedResult is not null)
        {
            LogCacheHit(logger, requestName);
            return cachedResult;
        }

        LogCacheMiss(logger, requestName);

        TResponse result = await next(cancellationToken);

        if (result.IsSuccess)
        {
            await cacheService.SetAsync(request.CacheKey, result, request.Expiration, cancellationToken);
        }

        return result;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cache hit for {RequestName}")]
    private static partial void LogCacheHit(ILogger logger, string requestName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cache miss for {RequestName}")]
    private static partial void LogCacheMiss(ILogger logger, string requestName);
}
