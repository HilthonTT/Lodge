using System.Data;
using Lodge.Application.Abstractions.App;
using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Users;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Users.GetById;

internal sealed class GetUserByIdQueryHandler(
    IDbConnectionFactory factory,
    IUserIdentifierProvider userIdentifierProvider,
    IOptions<AppOptions> appOptions) : IQueryHandler<GetUserByIdQuery, UserResponse>
{
    public async Task<Result<UserResponse>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        if (request.UserId != userIdentifierProvider.UserId)
        {
            return Result.Failure<UserResponse>(UserErrors.InvalidPermissions);
        }

        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        UserResponse? user = await UserQueries.GetByIdAsync(connection, request.UserId, appOptions.Value);

        return user is null
            ? Result.Failure<UserResponse>(UserErrors.NotFound(request.UserId))
            : user;
    }
}
