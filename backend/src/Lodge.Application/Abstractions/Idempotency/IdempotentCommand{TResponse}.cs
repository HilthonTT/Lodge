using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Abstractions.Idempotency;

/// <summary>
/// Represents the idempotent command.
/// </summary>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <param name="RequestId">The request identifier.</param>
public abstract record IdempotentCommand<TResponse>(Guid RequestId) : ICommand<TResponse>, IBaseIdempotentCommand;
