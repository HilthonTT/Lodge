using System.Data;
using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Common;
using Lodge.Contracts.Notifications;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Notifications;

internal sealed class GetNotificationsQueryHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IDbConnectionFactory factory) : IQueryHandler<GetNotificationsQuery, PagedList<NotificationResponse>>
{
    public async Task<Result<PagedList<NotificationResponse>>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        (List<NotificationResponse> items, int totalCount) = await NotificationQueries.GetPageAsync(
            connection,
            userIdentifierProvider.UserId,
            request.UnreadOnly,
            request.Page,
            request.PageSize);

        return new PagedList<NotificationResponse>(items, request.Page, request.PageSize, totalCount);
    }
}
