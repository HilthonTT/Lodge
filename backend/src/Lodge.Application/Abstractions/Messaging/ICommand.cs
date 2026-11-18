using Lodge.Domain.Core.Primitives;
using MediatR;

namespace Lodge.Application.Abstractions.Messaging;

/// <summary>
/// Represents the command interface.
/// </summary>
public interface ICommand : IRequest<Result>, IBaseCommand;
