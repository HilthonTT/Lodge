using System.Data;
using System.Globalization;
using FluentValidation;
using Lodge.Application.Abstractions.App;
using Lodge.Application.Abstractions.Assets;
using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Abstractions.Realtime;
using Lodge.Application.Core.Extensions;
using Lodge.Contracts.Common;
using Lodge.Contracts.Messaging;
using Lodge.Domain.Apartments;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Messaging;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Messaging;

/// <summary>
/// A guest's inquiry about a listing (<c>POST /conversations</c>): reuses the conversation of the guest and the listing
/// or starts it, and posts the message. Not allowed on one's own listing.
/// </summary>
public sealed record StartConversationCommand(Guid ApartmentId, string Message) : ICommand<SentMessageResponse>;

internal sealed class StartConversationCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IApartmentRepository apartmentRepository,
    ConversationService conversationService,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<StartConversationCommand, SentMessageResponse>
{
    public async Task<Result<SentMessageResponse>> Handle(StartConversationCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userIdentifierProvider.UserId;

        Result<string> body = Message.NormalizeBody(request.Message);

        if (body.IsFailure)
        {
            return Result.Failure<SentMessageResponse>(body.Error);
        }

        Apartment? apartment = await apartmentRepository.GetByIdAsync(request.ApartmentId, cancellationToken);

        if (apartment is null)
        {
            return Result.Failure<SentMessageResponse>(ApartmentErrors.NotFound(request.ApartmentId));
        }

        if (apartment.IsOwnedBy(userId))
        {
            return Result.Failure<SentMessageResponse>(MessagingErrors.OwnListing);
        }

        DateTime utcNow = dateTimeProvider.UtcNow;

        Result<Conversation> conversation =
            await conversationService.GetOrStartAsync(apartment.Id, userId, utcNow, cancellationToken);

        if (conversation.IsFailure)
        {
            return Result.Failure<SentMessageResponse>(conversation.Error);
        }

        Result<Message> message = conversationService.Send(conversation.Value, userId, body.Value, utcNow);

        if (message.IsFailure)
        {
            return Result.Failure<SentMessageResponse>(message.Error);
        }

        return new SentMessageResponse(conversation.Value.Id, MessageMapper.ToResponse(message.Value));
    }
}

/// <summary>
/// Sends a message in a conversation the caller takes part in (<c>POST /conversations/{id}/messages</c>).
/// </summary>
public sealed record SendMessageCommand(Guid ConversationId, string Body) : ICommand<MessageResponse>;

internal sealed class SendMessageCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IConversationRepository conversationRepository,
    ConversationService conversationService,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<SendMessageCommand, MessageResponse>
{
    public async Task<Result<MessageResponse>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userIdentifierProvider.UserId;

        Conversation? conversation = await conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);

        if (conversation is null || !conversation.IsParticipant(userId))
        {
            return Result.Failure<MessageResponse>(MessagingErrors.ConversationNotFound(request.ConversationId));
        }

        Result<Message> message = conversationService.Send(conversation, userId, request.Body, dateTimeProvider.UtcNow);

        return message.IsSuccess
            ? MessageMapper.ToResponse(message.Value)
            : Result.Failure<MessageResponse>(message.Error);
    }
}

/// <summary>
/// Moves the caller's read marker of a conversation to now (<c>PUT /conversations/{id}/read</c>).
/// </summary>
public sealed record MarkConversationReadCommand(Guid ConversationId) : ICommand;

internal sealed class MarkConversationReadCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IConversationRepository conversationRepository,
    IRealtimeOutbox realtimeOutbox,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<MarkConversationReadCommand>
{
    public async Task<Result> Handle(MarkConversationReadCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userIdentifierProvider.UserId;

        Conversation? conversation = await conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);

        if (conversation is null || !conversation.IsParticipant(userId))
        {
            return Result.Failure(MessagingErrors.ConversationNotFound(request.ConversationId));
        }

        conversation.MarkRead(userId, dateTimeProvider.UtcNow);
        realtimeOutbox.ConversationChanged(conversation.Id);

        return Result.Success();
    }
}

/// <summary>
/// Gets one page of the caller's conversations, newest message first (<c>GET /conversations</c>).
/// </summary>
public sealed record GetConversationsQuery(int Page, int PageSize) : IQuery<PagedList<ConversationResponse>>;

internal sealed class GetConversationsQueryValidator : AbstractValidator<GetConversationsQuery>
{
    public GetConversationsQueryValidator()
    {
        RuleFor(x => x.Page).ValidPage();
        RuleFor(x => x.PageSize).ValidPageSize();
    }
}

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

/// <summary>
/// Gets one of the caller's conversations (<c>GET /conversations/{id}</c>).
/// </summary>
public sealed record GetConversationByIdQuery(Guid ConversationId) : IQuery<ConversationResponse>;

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

/// <summary>
/// Gets a page of messages of one of the caller's conversations (<c>GET /conversations/{id}/messages</c>): the newest
/// <paramref name="Limit"/> messages older than <paramref name="Before"/> (a message id or an ISO 8601 date and time;
/// none = the latest), in chronological order.
/// </summary>
public sealed record GetMessagesQuery(Guid ConversationId, string? Before, int Limit) : IQuery<MessagePageResponse>;

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

/// <summary>
/// Counts the caller's unread messages across conversations (<c>GET /conversations/unread-count</c>).
/// </summary>
public sealed record GetUnreadMessageCountQuery : IQuery<UnreadCountResponse>;

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

internal static class MessageMapper
{
    public static MessageResponse ToResponse(Message message) => new()
    {
        Id = message.Id,
        ConversationId = message.ConversationId,
        SenderId = message.SenderId,
        Type = message.Type.ToString(),
        Body = message.Body,
        SystemEvent = message.SystemEvent,
        BookingId = message.BookingId,
        SentOnUtc = message.SentOnUtc,
    };
}
