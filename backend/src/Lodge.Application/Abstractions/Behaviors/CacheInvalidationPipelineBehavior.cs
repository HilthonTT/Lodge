using Lodge.Application.Abstractions.Caching;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Core.Primitives;
using MediatR;

namespace Lodge.Application.Abstractions.Behaviors;

/// <summary>
/// Removes the cache keys registered through <see cref="ICacheInvalidator"/> once a command has succeeded.
/// Registered outside <see cref="UnitOfWorkPipelineBehavior{TRequest,TResponse}"/>, so it runs after the commit.
/// </summary>
internal sealed class CacheInvalidationPipelineBehavior<TRequest, TResponse>(
    CacheInvalidator cacheInvalidator,
    ICacheService cacheService)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseCommand
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        TResponse response = await next(cancellationToken);

        IReadOnlyList<string> keys = cacheInvalidator.Drain();

        if (response.IsSuccess)
        {
            foreach (string key in keys)
            {
                await cacheService.RemoveAsync(key, cancellationToken);
            }
        }

        return response;
    }
}
