using System.Data;
using System.Globalization;
using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Messaging;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Messaging;

namespace Lodge.Application.Messaging;

internal sealed class GetMessagesQueryHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IDbConnectionFactory factory) : IQueryHandler<GetMessagesQuery, MessagePageResponse>
{
    public async Task<Result<MessagePageResponse>> Handle(GetMessagesQuery request, CancellationToken cancellationToken)
    {
        if (request.Limit is < 1 or > Message.MaxPageSize)
        {
            return Result.Failure<MessagePageResponse>(MessagingErrors.InvalidLimit);
        }

        Guid userId = userIdentifierProvider.UserId;

        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        (Guid GuestId, Guid HostId)? participants =
            await MessagingQueries.GetParticipantsAsync(connection, request.ConversationId);

        if (participants is null || (participants.Value.GuestId != userId && participants.Value.HostId != userId))
        {
            return Result.Failure<MessagePageResponse>(MessagingErrors.ConversationNotFound(request.ConversationId));
        }

        DateTime? beforeUtc = null;
        Guid? beforeId = null;

        if (!string.IsNullOrWhiteSpace(request.Before))
        {
            if (Guid.TryParse(request.Before, out Guid messageId))
            {
                MessageResponse? cursor = await MessagingQueries.GetMessageAsync(connection, messageId);

                if (cursor is null || cursor.ConversationId != request.ConversationId)
                {
                    return Result.Failure<MessagePageResponse>(MessagingErrors.InvalidCursor);
                }

                beforeUtc = cursor.SentOnUtc;
                beforeId = cursor.Id;
            }
            else if (DateTime.TryParse(
                         request.Before,
                         CultureInfo.InvariantCulture,
                         DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                         out DateTime before))
            {
                beforeUtc = DateTime.SpecifyKind(before, DateTimeKind.Utc);
            }
            else
            {
                return Result.Failure<MessagePageResponse>(MessagingErrors.InvalidCursor);
            }
        }

        List<MessageResponse> newestFirst = await MessagingQueries.GetMessagesBeforeAsync(
            connection,
            request.ConversationId,
            beforeUtc,
            beforeId,
            request.Limit + 1);

        bool hasMore = newestFirst.Count > request.Limit;

        List<MessageResponse> page = [.. newestFirst.Take(request.Limit).Reverse()];

        return new MessagePageResponse(page, hasMore, hasMore ? page[0].Id : null);
    }
}
