using Lodge.Domain.Core.Primitives;
using MediatR;

namespace Lodge.Application.Abstractions.Messaging;

/// <summary>
/// Represents the command interface.
/// </summary>
/// <typeparam name="TResponse">The command response type.</typeparam>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>, IBaseCommand;
