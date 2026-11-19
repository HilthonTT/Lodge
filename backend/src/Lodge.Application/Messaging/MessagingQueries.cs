using System.Data;
using Dapper;
using Lodge.Application.Abstractions.App;
using Lodge.Application.Abstractions.Assets;
using Lodge.Contracts.Messaging;
using Lodge.Domain.Bookings;

namespace Lodge.Application.Messaging;

/// <summary>
/// Dapper read queries of conversations and messages.
/// </summary>
internal static class MessagingQueries
{
    private const string MessageColumns =
        """
        m.id AS Id,
        m.conversation_id AS ConversationId,
        m.sender_id AS SenderId,
        m.type AS Type,
        m.body AS Body,
        m.system_event AS SystemEvent,
        m.booking_id AS BookingId,
        m.sent_on_utc AS SentOnUtc
        """;

    /// <summary>
    /// The unread messages of the user in a conversation <c>c</c>: from the other participant or the system, after
    /// the user's read marker.
    /// </summary>
    private const string UnreadCondition =
        """
        m.conversation_id = c.id
        AND m.sender_id IS DISTINCT FROM @UserId
        AND m.sent_on_utc > COALESCE(
            CASE WHEN c.guest_id = @UserId THEN c.guest_last_read_on_utc ELSE c.host_last_read_on_utc END,
            '-infinity'::timestamptz)
        """;

    public static Task<MessageResponse?> GetMessageAsync(IDbConnection connection, Guid messageId) =>
        connection.QueryFirstOrDefaultAsync<MessageResponse>(
            $"""
            SELECT {MessageColumns}
            FROM messages AS m
            WHERE m.id = @MessageId
            """,
            new { MessageId = messageId });

    public static async Task<MessageWithParticipants?> GetMessageWithParticipantsAsync(IDbConnection connection, Guid messageId)
    {
        MessageRow? row = await connection.QueryFirstOrDefaultAsync<MessageRow>(
            $"""
            SELECT {MessageColumns}, c.guest_id AS GuestId, c.host_id AS HostId
            FROM messages AS m
            INNER JOIN conversations AS c ON c.id = m.conversation_id
            WHERE m.id = @MessageId
            """,
            new { MessageId = messageId });

        return row is null ? null : new MessageWithParticipants(row.ToResponse(), row.GuestId, row.HostId);
    }

    public static async Task<(Guid GuestId, Guid HostId)?> GetParticipantsAsync(IDbConnection connection, Guid conversationId)
    {
        ParticipantsRow? row = await connection.QueryFirstOrDefaultAsync<ParticipantsRow>(
            "SELECT guest_id AS GuestId, host_id AS HostId FROM conversations WHERE id = @ConversationId",
            new { ConversationId = conversationId });

        return row is null ? null : (row.GuestId, row.HostId);
    }

    public static Task<int> CountUnreadMessagesAsync(IDbConnection connection, Guid userId) =>
        connection.ExecuteScalarAsync<int>(
            $"""
            SELECT COUNT(*)::int
            FROM conversations AS c
            INNER JOIN messages AS m ON {UnreadCondition}
            WHERE c.guest_id = @UserId OR c.host_id = @UserId
            """,
            new { UserId = userId });

    /// <summary>
    /// Gets one page of the user's conversations (newest message first), or a single one.
    /// </summary>
    public static async Task<(List<ConversationResponse> Items, int TotalCount)> GetConversationsAsync(
        IDbConnection connection,
        Guid userId,
        int page,
        int pageSize,
        Guid? conversationId,
        AppOptions appOptions,
        IAssetUrlResolver assetUrlResolver)
    {
        string where = conversationId is null
            ? "(c.guest_id = @UserId OR c.host_id = @UserId)"
            : "(c.guest_id = @UserId OR c.host_id = @UserId) AND c.id = @ConversationId";

        string sql =
            $"""
            SELECT COUNT(*)::int FROM conversations AS c WHERE {where};

            SELECT
                c.id AS Id,
                c.last_message_on_utc AS LastMessageOnUtc,
                a.id AS ApartmentId,
                a.name AS ApartmentName,
                (SELECT COALESCE(i.path, '/api/v1/files/' || i.id::text)
                 FROM apartment_images AS i
                 WHERE i.apartment_id = a.id
                 ORDER BY i.position
                 LIMIT 1) AS ApartmentImageUrl,
                o.id AS OtherId,
                CASE WHEN c.guest_id = @UserId THEN 'Host' ELSE 'Guest' END AS OtherRole,
                o.first_name AS OtherFirstName,
                o.image_id AS OtherImageId,
                lm.body AS LastBody,
                lm.type AS LastType,
                lm.sent_on_utc AS LastSentOnUtc,
                lm.sender_id AS LastSenderId,
                lm.system_event AS LastSystemEvent,
                (SELECT COUNT(*)::int FROM messages AS m WHERE {UnreadCondition}) AS UnreadCount,
                lb.id AS BookingId,
                lb.status AS BookingStatus,
                lb.duration_start::timestamp AS BookingStart,
                lb.duration_end::timestamp AS BookingEnd
            FROM conversations AS c
            INNER JOIN apartments AS a ON a.id = c.apartment_id
            INNER JOIN users AS o ON o.id = CASE WHEN c.guest_id = @UserId THEN c.host_id ELSE c.guest_id END
            LEFT JOIN LATERAL (
                SELECT m.body, m.type, m.sent_on_utc, m.sender_id, m.system_event
                FROM messages AS m
                WHERE m.conversation_id = c.id
                ORDER BY m.sent_on_utc DESC, m.id DESC
                LIMIT 1
            ) AS lm ON TRUE
            LEFT JOIN LATERAL (
                SELECT b.id, b.status, b.duration_start, b.duration_end
                FROM bookings AS b
                WHERE b.apartment_id = c.apartment_id AND b.user_id = c.guest_id
                ORDER BY b.created_on_utc DESC, b.id
                LIMIT 1
            ) AS lb ON TRUE
            WHERE {where}
            ORDER BY c.last_message_on_utc DESC, c.id
            LIMIT @PageSize OFFSET @Offset;
            """;

        using SqlMapper.GridReader grid = await connection.QueryMultipleAsync(
            sql,
            new
            {
                UserId = userId,
                ConversationId = conversationId ?? Guid.Empty,
                PageSize = pageSize,
                Offset = (page - 1L) * pageSize,
            });

        int totalCount = await grid.ReadSingleAsync<int>();

        List<ConversationResponse> items =
        [
            .. (await grid.ReadAsync<ConversationRow>()).Select(row => new ConversationResponse
            {
                Id = row.Id,
                LastMessageOnUtc = row.LastMessageOnUtc,
                Apartment = new ConversationApartmentResponse(
                    row.ApartmentId,
                    row.ApartmentName,
                    row.ApartmentImageUrl is null ? null : assetUrlResolver.Resolve(row.ApartmentImageUrl)),
                OtherParticipant = new ConversationParticipantResponse(
                    row.OtherId,
                    row.OtherFirstName,
                    row.OtherImageId is Guid imageId ? appOptions.FileUrl(imageId) : null,
                    row.OtherRole),
                LastMessage = row.LastSentOnUtc is DateTime sentOnUtc
                    ? new LastMessageResponse(row.LastBody ?? string.Empty, row.LastType ?? string.Empty, sentOnUtc, row.LastSenderId, row.LastSystemEvent)
                    : null,
                UnreadCount = row.UnreadCount,
                LatestBooking = row.BookingId is Guid bookingId
                    ? new ConversationBookingResponse(
                        bookingId,
                        ((BookingStatus)(row.BookingStatus ?? 0)).ToString(),
                        DateOnly.FromDateTime(row.BookingStart!.Value),
                        DateOnly.FromDateTime(row.BookingEnd!.Value))
                    : null,
            }),
        ];

        return (items, totalCount);
    }

    /// <summary>
    /// Gets up to <paramref name="take"/> messages older than the cursor, newest first.
    /// </summary>
    public static async Task<List<MessageResponse>> GetMessagesBeforeAsync(
        IDbConnection connection,
        Guid conversationId,
        DateTime? beforeUtc,
        Guid? beforeId,
        int take)
    {
        string cursor = (beforeUtc, beforeId) switch
        {
            (not null, not null) => "AND (m.sent_on_utc, m.id) < (@BeforeUtc, @BeforeId)",
            (not null, null) => "AND m.sent_on_utc < @BeforeUtc",
            _ => string.Empty,
        };

        IEnumerable<MessageResponse> messages = await connection.QueryAsync<MessageResponse>(
            $"""
            SELECT {MessageColumns}
            FROM messages AS m
            WHERE m.conversation_id = @ConversationId {cursor}
            ORDER BY m.sent_on_utc DESC, m.id DESC
            LIMIT @Take
            """,
            new
            {
                ConversationId = conversationId,
                BeforeUtc = beforeUtc ?? DateTime.MaxValue,
                BeforeId = beforeId ?? Guid.Empty,
                Take = take,
            });

        return [.. messages];
    }

    internal sealed record MessageWithParticipants(MessageResponse Message, Guid GuestId, Guid HostId);

    private sealed class MessageRow
    {
        public Guid Id { get; init; }

        public Guid ConversationId { get; init; }

        public Guid? SenderId { get; init; }

        public string Type { get; init; } = string.Empty;

        public string Body { get; init; } = string.Empty;

        public string? SystemEvent { get; init; }

        public Guid? BookingId { get; init; }

        public DateTime SentOnUtc { get; init; }

        public Guid GuestId { get; init; }

        public Guid HostId { get; init; }

        public MessageResponse ToResponse() => new()
        {
            Id = Id,
            ConversationId = ConversationId,
            SenderId = SenderId,
            Type = Type,
            Body = Body,
            SystemEvent = SystemEvent,
            BookingId = BookingId,
            SentOnUtc = SentOnUtc,
        };
    }

    private sealed class ParticipantsRow
    {
        public Guid GuestId { get; init; }

        public Guid HostId { get; init; }
    }

    private sealed class ConversationRow
    {
        public Guid Id { get; init; }

        public DateTime LastMessageOnUtc { get; init; }

        public Guid ApartmentId { get; init; }

        public string ApartmentName { get; init; } = string.Empty;

        public string? ApartmentImageUrl { get; init; }

        public Guid OtherId { get; init; }

        public string OtherRole { get; init; } = string.Empty;

        public string OtherFirstName { get; init; } = string.Empty;

        public Guid? OtherImageId { get; init; }

        public string? LastBody { get; init; }

        public string? LastType { get; init; }

        public DateTime? LastSentOnUtc { get; init; }

        public Guid? LastSenderId { get; init; }

        public string? LastSystemEvent { get; init; }

        public int UnreadCount { get; init; }

        public Guid? BookingId { get; init; }

        public int? BookingStatus { get; init; }

        public DateTime? BookingStart { get; init; }

        public DateTime? BookingEnd { get; init; }
    }
}
