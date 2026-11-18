using Lodge.Domain.Core.Primitives;
using MediatR;

namespace Lodge.Application.Abstractions.Messaging;

public interface ICommandHandler<in TCommand, TResponse>
    : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>
{
}
