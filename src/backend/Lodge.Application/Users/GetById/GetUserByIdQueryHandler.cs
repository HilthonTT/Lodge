using Dapper;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Users;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;
using System.Data;

namespace Lodge.Application.Users.GetById;

/// <summary>
/// Represents the <see cref="GetUserByIdQuery"/> handler.
/// </summary>
/// <param name="factory">The database connection factory.</param>
internal sealed class GetUserByIdQueryHandler(IDbConnectionFactory factory) 
    : IQueryHandler<GetUserByIdQuery, UserResponse>
{
    /// <inheritdoc />
    public async Task<Result<UserResponse>> Handle(
        GetUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        UserResponse? user = await UserQueries.GetByIdAsync(connection, request.UserId);

        if (user is null)
        {
            return Result.Failure<UserResponse>(UserErrors.NotFound(request.UserId));
        }

        return user;
    }
}
