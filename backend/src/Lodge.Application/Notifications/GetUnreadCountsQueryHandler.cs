using System.Data;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Notifications;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Notifications;

internal sealed class GetUnreadCountsQueryHandler(IDbConnectionFactory factory)
    : IQueryHandler<GetUnreadCountsQuery, UnreadCountsResponse>
{
    public async Task<Result<UnreadCountsResponse>> Handle(GetUnreadCountsQuery request, CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        return await NotificationQueries.GetUnreadCountsAsync(connection, request.UserId);
    }
}
