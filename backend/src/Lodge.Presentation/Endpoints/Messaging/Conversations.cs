using Lodge.Application.Messaging;
using Lodge.Contracts.Common;
using Lodge.Contracts.Messaging;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Messaging;
using Lodge.Presentation.Extensions;
using Lodge.Presentation.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lodge.Presentation.Endpoints.Messaging;

/// <summary>
/// Messaging between guests and hosts. Conversations the caller does not take part in are "not found" (404).
/// </summary>
internal sealed class Conversations : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("conversations", async (
            StartConversationRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<SentMessageResponse> result = await sender.Send(
                new StartConversationCommand(request.ApartmentId, request.Message),
                cancellationToken);

            return result.Match(
                sent => Results.Created($"/api/v1/conversations/{sent.ConversationId}", sent),
                CustomResults.Problem);
        })
        .WithTags(Tags.Messaging)
        .RequireAuthorization();

        app.MapGet("conversations", async (
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<PagedList<ConversationResponse>> result = await sender.Send(
                new GetConversationsQuery(page ?? Paging.DefaultPage, pageSize ?? Paging.DefaultPageSize),
                cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Messaging)
        .RequireAuthorization();

        app.MapGet("conversations/unread-count", async (ISender sender, CancellationToken cancellationToken) =>
        {
            Result<UnreadCountResponse> result = await sender.Send(new GetUnreadMessageCountQuery(), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Messaging)
        .RequireAuthorization();

        app.MapGet("conversations/{conversationId:guid}", async (
            Guid conversationId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<ConversationResponse> result = await sender.Send(new GetConversationByIdQuery(conversationId), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Messaging)
        .RequireAuthorization();

        app.MapGet("conversations/{conversationId:guid}/messages", async (
            Guid conversationId,
            [FromQuery] string? before,
            [FromQuery] int? limit,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<MessagePageResponse> result = await sender.Send(
                new GetMessagesQuery(conversationId, before, limit ?? Message.DefaultPageSize),
                cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Messaging)
        .RequireAuthorization();

        app.MapPost("conversations/{conversationId:guid}/messages", async (
            Guid conversationId,
            SendMessageRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<MessageResponse> result = await sender.Send(
                new SendMessageCommand(conversationId, request.Body),
                cancellationToken);

            return result.Match(
                message => Results.Created($"/api/v1/conversations/{conversationId}/messages", message),
                CustomResults.Problem);
        })
        .WithTags(Tags.Messaging)
        .RequireAuthorization();

        app.MapPut("conversations/{conversationId:guid}/read", async (
            Guid conversationId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result result = await sender.Send(new MarkConversationReadCommand(conversationId), cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Messaging)
        .RequireAuthorization();
    }
}
