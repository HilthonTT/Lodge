using Lodge.Application.Notifications;
using Lodge.Application.Notifications.Get;
using Lodge.Application.Notifications.GetPreferences;
using Lodge.Application.Notifications.GetUnreadCount;
using Lodge.Application.Notifications.MarkAllRead;
using Lodge.Application.Notifications.MarkRead;
using Lodge.Application.Notifications.UpdatePreferences;
using Lodge.Contracts.Common;
using Lodge.Contracts.Messaging;
using Lodge.Contracts.Notifications;
using Lodge.Domain.Core.Primitives;
using Lodge.Presentation.Extensions;
using Lodge.Presentation.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lodge.Presentation.Endpoints.Notifications;

/// <summary>
/// The caller's in-app notifications and notification preferences.
/// </summary>
internal sealed class Notifications : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("notifications", async (
            [FromQuery] bool? unreadOnly,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<PagedList<NotificationResponse>> result = await sender.Send(
                new GetNotificationsQuery(unreadOnly ?? false, page ?? Paging.DefaultPage, pageSize ?? Paging.DefaultPageSize),
                cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Notifications)
        .RequireAuthorization();

        app.MapGet("notifications/unread-count", async (ISender sender, CancellationToken cancellationToken) =>
        {
            Result<UnreadCountResponse> result = await sender.Send(new GetUnreadNotificationCountQuery(), cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Notifications)
        .RequireAuthorization();

        app.MapPut("notifications/read-all", async (ISender sender, CancellationToken cancellationToken) =>
        {
            Result result = await sender.Send(new MarkAllNotificationsReadCommand(), cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Notifications)
        .RequireAuthorization();

        app.MapPut("notifications/{notificationId:guid}/read", async (
            Guid notificationId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result result = await sender.Send(new MarkNotificationReadCommand(notificationId), cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Notifications)
        .RequireAuthorization();

        app.MapGet("users/{userId:guid}/notification-preferences", async (
            Guid userId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<NotificationPreferencesDto> result = await sender.Send(
                new GetNotificationPreferencesQuery(userId),
                cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Notifications)
        .RequireAuthorization();

        app.MapPut("users/{userId:guid}/notification-preferences", async (
            Guid userId,
            NotificationPreferencesDto request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            Result<NotificationPreferencesDto> result = await sender.Send(
                new UpdateNotificationPreferencesCommand(userId, request),
                cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Notifications)
        .RequireAuthorization();
    }
}
