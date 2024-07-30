using Lodge.Domain.Core.Primitives;
using MediatR;

namespace Lodge.Application.Abstractions.Messaging;

/// <summary>
/// Represents the command interface.
/// </summary>
public interface ICommand : IRequest<Result>, IBaseCommand;

/// <summary>
/// Represents the command interface.
/// </summary>
/// <typeparam name="TResponse">The command response type.</typeparam>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>, IBaseCommand;

/// <summary>
/// Represents the base command interface.
/// </summary>
public interface IBaseCommand;
