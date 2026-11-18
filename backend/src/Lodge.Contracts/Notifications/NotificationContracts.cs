using System.Text.Json;

namespace Lodge.Contracts.Notifications;

/// <summary>
/// An in-app notification. The frontend renders it from <see cref="Type"/> and <see cref="Data"/> (a JSON object);
/// <see cref="Link"/> is a relative frontend path.
/// </summary>
public sealed record NotificationResponse
{
    public Guid Id { get; init; }

    public string Type { get; init; } = string.Empty;

    public JsonElement Data { get; init; }

    public string Link { get; init; } = string.Empty;

    public DateTime CreatedOnUtc { get; init; }

    public DateTime? ReadOnUtc { get; init; }
}

/// <summary>
/// The unread counters pushed over SignalR (<c>unreadCounts</c>).
/// </summary>
/// <param name="Messages">Unread messages across all conversations.</param>
/// <param name="Notifications">Unread in-app notifications.</param>
public sealed record UnreadCountsResponse(int Messages, int Notifications);

/// <summary>
/// The notification settings (<c>GET</c>/<c>PUT /users/{userId}/notification-preferences</c>).
/// </summary>
public sealed record NotificationPreferencesDto(NotificationChannelDto Email, NotificationChannelDto InApp);

/// <summary>
/// The categories of one channel; all true by default.
/// </summary>
public sealed record NotificationChannelDto(bool Messages, bool Reservations, bool Reviews, bool Payouts, bool Reminders);
