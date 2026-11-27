using System.Data;
using Lodge.Application.Abstractions.App;
using Lodge.Application.Abstractions.Assets;
using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Messaging;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Messaging;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Messaging.GetConversationById;

internal sealed class GetConversationByIdQueryHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IDbConnectionFactory factory,
    IOptions<AppOptions> appOptions,
    IAssetUrlResolver assetUrlResolver) : IQueryHandler<GetConversationByIdQuery, ConversationResponse>
{
    public async Task<Result<ConversationResponse>> Handle(GetConversationByIdQuery request, CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        (List<ConversationResponse> items, _) = await MessagingQueries.GetConversationsAsync(
            connection,
            userIdentifierProvider.UserId,
            1,
            1,
            request.ConversationId,
            appOptions.Value,
            assetUrlResolver);

        return items.Count == 0
            ? Result.Failure<ConversationResponse>(MessagingErrors.ConversationNotFound(request.ConversationId))
            : items[0];
    }
}
