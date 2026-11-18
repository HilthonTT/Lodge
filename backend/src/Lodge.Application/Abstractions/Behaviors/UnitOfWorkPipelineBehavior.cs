using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Core.Primitives;
using MediatR;
using Microsoft.EntityFrameworkCore.Storage;

namespace Lodge.Application.Abstractions.Behaviors;

/// <summary>
/// Wraps every command in a database transaction. The transaction is committed only when the
/// command succeeds; failed results and exceptions roll back every change (including idempotency records).
/// </summary>
internal sealed class UnitOfWorkPipelineBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseCommand
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        await using IDbContextTransaction transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        TResponse response = await next(cancellationToken);

        if (response.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            unitOfWork.DiscardChanges();

            return response;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return response;
    }
}
