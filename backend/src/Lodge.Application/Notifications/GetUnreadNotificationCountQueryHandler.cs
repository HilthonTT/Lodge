using System.Data;
using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Messaging;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Notifications;

internal sealed class GetUnreadNotificationCountQueryHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IDbConnectionFactory factory) : IQueryHandler<GetUnreadNotificationCountQuery, UnreadCountResponse>
{
    public async Task<Result<UnreadCountResponse>> Handle(
        GetUnreadNotificationCountQuery request,
        CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        return new UnreadCountResponse(await NotificationQueries.CountUnreadAsync(connection, userIdentifierProvider.UserId));
    }
}
