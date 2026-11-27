using System.Data;
using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Messaging;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Messaging.GetUnreadCount;

internal sealed class GetUnreadMessageCountQueryHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IDbConnectionFactory factory) : IQueryHandler<GetUnreadMessageCountQuery, UnreadCountResponse>
{
    public async Task<Result<UnreadCountResponse>> Handle(GetUnreadMessageCountQuery request, CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        return new UnreadCountResponse(
            await MessagingQueries.CountUnreadMessagesAsync(connection, userIdentifierProvider.UserId));
    }
}
