using System.Data;
using System.Text.Json;
using Dapper;
using Lodge.Application.Messaging;
using Lodge.Contracts.Notifications;
using Lodge.Domain.Notifications;

namespace Lodge.Application.Notifications;

/// <summary>
/// Dapper queries of notifications.
/// </summary>
internal static class NotificationQueries
{
    private const string Columns =
        """
        n.id AS Id,
        n.user_id AS UserId,
        n.type AS Type,
        n.data::text AS Data,
        n.link AS Link,
        n.created_on_utc AS CreatedOnUtc,
        n.read_on_utc AS ReadOnUtc
        """;

    /// <summary>
    /// Inserts the notification unless the user already has one with the same dedupe key.
    /// </summary>
    /// <returns><see langword="true"/> when the notification was inserted.</returns>
    public static async Task<bool> TryInsertAsync(IDbConnection connection, Notification notification)
    {
        Guid? id = await connection.ExecuteScalarAsync<Guid?>(
            """
            INSERT INTO notifications (id, user_id, type, data, link, dedupe_key, in_app, created_on_utc)
            VALUES (@Id, @UserId, @Type, CAST(@Data AS jsonb), @Link, @DedupeKey, @InApp, @CreatedOnUtc)
            ON CONFLICT (user_id, dedupe_key) DO NOTHING
            RETURNING id
            """,
            new
            {
                notification.Id,
                notification.UserId,
                notification.Type,
                notification.Data,
                notification.Link,
                notification.DedupeKey,
                notification.InApp,
                notification.CreatedOnUtc,
            });

        return id is not null;
    }

    public static async Task<(Guid UserId, NotificationResponse Notification)?> GetForPushAsync(
        IDbConnection connection,
        Guid notificationId)
    {
        NotificationRow? row = await connection.QueryFirstOrDefaultAsync<NotificationRow>(
            $"""
            SELECT {Columns}
            FROM notifications AS n
            WHERE n.id = @NotificationId AND n.in_app
            """,
            new { NotificationId = notificationId });

        return row is null ? null : (row.UserId, row.ToResponse());
    }

    public static Task<int> CountUnreadAsync(IDbConnection connection, Guid userId) =>
        connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*)::int FROM notifications WHERE user_id = @UserId AND in_app AND read_on_utc IS NULL",
            new { UserId = userId });

    public static async Task<UnreadCountsResponse> GetUnreadCountsAsync(IDbConnection connection, Guid userId) =>
        new(
            await MessagingQueries.CountUnreadMessagesAsync(connection, userId),
            await CountUnreadAsync(connection, userId));

    public static async Task<(List<NotificationResponse> Items, int TotalCount)> GetPageAsync(
        IDbConnection connection,
        Guid userId,
        bool unreadOnly,
        int page,
        int pageSize)
    {
        string where = unreadOnly
            ? "n.user_id = @UserId AND n.in_app AND n.read_on_utc IS NULL"
            : "n.user_id = @UserId AND n.in_app";

        using SqlMapper.GridReader grid = await connection.QueryMultipleAsync(
            $"""
            SELECT COUNT(*)::int FROM notifications AS n WHERE {where};

            SELECT {Columns}
            FROM notifications AS n
            WHERE {where}
            ORDER BY n.created_on_utc DESC, n.id
            LIMIT @PageSize OFFSET @Offset;
            """,
            new { UserId = userId, PageSize = pageSize, Offset = (page - 1) * pageSize });

        int totalCount = await grid.ReadSingleAsync<int>();
        List<NotificationResponse> items = [.. (await grid.ReadAsync<NotificationRow>()).Select(row => row.ToResponse())];

        return (items, totalCount);
    }

    private sealed class NotificationRow
    {
        public Guid Id { get; init; }

        public Guid UserId { get; init; }

        public string Type { get; init; } = string.Empty;

        public string Data { get; init; } = "{}";

        public string Link { get; init; } = string.Empty;

        public DateTime CreatedOnUtc { get; init; }

        public DateTime? ReadOnUtc { get; init; }

        public NotificationResponse ToResponse()
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(Data) ? "{}" : Data);

            return new NotificationResponse
            {
                Id = Id,
                Type = Type,
                Data = document.RootElement.Clone(),
                Link = Link,
                CreatedOnUtc = CreatedOnUtc,
                ReadOnUtc = ReadOnUtc,
            };
        }
    }
}
