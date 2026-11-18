using System.Data;
using Lodge.Application.Abstractions.App;
using Lodge.Application.Abstractions.Assets;
using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Common;
using Lodge.Contracts.Messaging;
using Lodge.Domain.Core.Primitives;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Messaging;

internal sealed class GetConversationsQueryHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IDbConnectionFactory factory,
    IOptions<AppOptions> appOptions,
    IAssetUrlResolver assetUrlResolver) : IQueryHandler<GetConversationsQuery, PagedList<ConversationResponse>>
{
    public async Task<Result<PagedList<ConversationResponse>>> Handle(
        GetConversationsQuery request,
        CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        (List<ConversationResponse> items, int totalCount) = await MessagingQueries.GetConversationsAsync(
            connection,
            userIdentifierProvider.UserId,
            request.Page,
            request.PageSize,
            conversationId: null,
            appOptions.Value,
            assetUrlResolver);

        return new PagedList<ConversationResponse>(items, request.Page, request.PageSize, totalCount);
    }
}
