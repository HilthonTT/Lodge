using Lodge.Application.Abstractions.Idempotency;
using Lodge.Application.Core.Errors;
using Lodge.Domain.Core.Primitives;
using MediatR;

namespace Lodge.Application.Abstractions.Behaviors;

/// <summary>
/// Rejects commands whose idempotency key has already been processed. It runs inside the unit of work,
/// so the idempotency record is only persisted when the command succeeds.
/// </summary>
internal sealed class IdempotentCommandPipelineBehavior<TRequest, TResponse>(
    IIdempotencyService idempotencyService) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseIdempotentCommand
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        bool created = await idempotencyService.TryCreateRequestAsync(
            request.RequestId,
            typeof(TRequest).Name,
            cancellationToken);

        if (!created)
        {
            return ResultFactory.Failure<TResponse>(IdempotencyErrors.AlreadyProcessed);
        }

        return await next(cancellationToken);
    }
}
