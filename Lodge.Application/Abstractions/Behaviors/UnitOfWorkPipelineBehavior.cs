using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore.Storage;

namespace Lodge.Application.Abstractions.Behaviors;

/// <summary>
/// Represents the unit of work pipeline serving a transactional pipeline middleware.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <param name="unitOfWork">The unit of work.</param>
internal sealed class UnitOfWorkPipelineBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseCommand
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        await using IDbContextTransaction transaction = await unitOfWork.BeginTransactionAsync(
            cancellationToken);

        try
        {
            TResponse? response = await next();

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return response;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);

            throw;
        }
    }
}
