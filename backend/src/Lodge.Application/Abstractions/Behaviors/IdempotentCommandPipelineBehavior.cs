using Lodge.Application.Abstractions.Idempotency;
using Lodge.Application.Core.Errors;
using Lodge.Domain.Core.Primitives;
using MediatR;

namespace Lodge.Application.Abstractions.Behaviors;

/// <summary>
/// Represents the idempotent request command pipeline behavior.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <param name="idempotencyService">The idempotency service.</param>
internal sealed class IdempotentCommandPipelineBehavior<TRequest, TResponse>(
    IIdempotencyService idempotencyService) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseIdempotentCommand
    where TResponse : Result
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (await idempotencyService.RequestExistsAsync(request.RequestId, cancellationToken))
        {
            return (TResponse)Result.Failure(IdempotencyErrors.AlreadyProcessed);
        }

        string requestName = typeof(TRequest).Name;

        await idempotencyService.CreateRequestAsync(request.RequestId, requestName, cancellationToken);

        TResponse response = await next();

        return response;
    }
}
