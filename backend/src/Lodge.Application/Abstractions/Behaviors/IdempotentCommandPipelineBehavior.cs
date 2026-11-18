using Lodge.Application.Abstractions.Idempotency;
using Lodge.Application.Core.Errors;
using Lodge.Domain.Core.Primitives;
using MediatR;

namespace Lodge.Application.Abstractions.Behaviors;

/// <summary>
/// Makes commands with an idempotency key safe to retry. It runs inside the unit of work:
/// <list type="bullet">
/// <item>a new key runs the command; on success the key is stored with the serialized result, in the same transaction
/// (a failed command rolls it back, so the key can be reused);</item>
/// <item>a key already used by the same request type replays the stored result without running the command;</item>
/// <item>a key used by another request type fails with <c>409 Request.KeyReused</c>;</item>
/// <item>a key whose first request is still running fails with <c>409 Request.InProgress</c>.</item>
/// </list>
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
        string name = typeof(TRequest).Name;

        IdempotencyCheck check = await idempotencyService.BeginAsync(request.RequestId, name, cancellationToken);

        switch (check.Status)
        {
            case IdempotencyStatus.InProgress:
                return ResultFactory.Failure<TResponse>(IdempotencyErrors.InProgress);
            case IdempotencyStatus.KeyReused:
                return ResultFactory.Failure<TResponse>(IdempotencyErrors.KeyReused);
            case IdempotencyStatus.Completed:
                return check.Response is null
                    ? ResultFactory.Failure<TResponse>(IdempotencyErrors.AlreadyProcessed)
                    : ResultFactory.Deserialize<TResponse>(check.Response);
        }

        TResponse response = await next(cancellationToken);

        if (response.IsSuccess)
        {
            idempotencyService.Complete(request.RequestId, name, ResultFactory.Serialize(response));
        }

        return response;
    }
}
