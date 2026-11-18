using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Abstractions.Idempotency;

/// <summary>
/// Represents the idempotent command.
/// </summary>
/// <param name="RequestId">The request identifier.</param>
public abstract record IdempotentCommand(Guid RequestId) : ICommand<Guid>, IBaseIdempotentCommand;
