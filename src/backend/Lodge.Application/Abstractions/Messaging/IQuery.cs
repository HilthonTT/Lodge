using Lodge.Domain.Core.Primitives;
using MediatR;

namespace Lodge.Application.Abstractions.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
