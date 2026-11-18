using Lodge.Application.Abstractions.App;
using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Users;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;
using Microsoft.Extensions.Options;
using System.Data;

namespace Lodge.Application.Users.GetFromAuth;

/// <summary>
/// Represents the <see cref="GetUserFromAuthQuery"/> query handler.
/// </summary>
/// <param name="factory">The database connection.</param>
/// <param name="userIdentifierProvider">The user identifier provider.</param>
internal sealed class GetUserFromAuthQueryHandler(
    IDbConnectionFactory factory, 
    IUserIdentifierProvider userIdentifierProvider,
    IOptions<AppOptions> appOptions) : IQueryHandler<GetUserFromAuthQuery, UserResponse>
{
    /// <inheritdoc />
    public async Task<Result<UserResponse>> Handle(GetUserFromAuthQuery request, CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        UserResponse? user = await UserQueries.GetByIdAsync(connection, userIdentifierProvider.UserId, appOptions.Value);

        if (user is null)
        {
            return Result.Failure<UserResponse>(UserErrors.NotFound(userIdentifierProvider.UserId));
        }

        return user;
    }
}
