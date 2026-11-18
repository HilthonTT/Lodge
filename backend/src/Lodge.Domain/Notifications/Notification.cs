using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Notifications;

/// <summary>
/// An in-app notification. It stores no English text: the frontend localises it from <see cref="Type"/> (see
/// <see cref="NotificationTypes"/>) and the values in <see cref="Data"/> (a JSON object).
/// </summary>
/// <remarks>
/// Notifications are created once per source event: <see cref="DedupeKey"/> is unique per user. A notification of a
/// category the user turned off in-app is still recorded (so that e-mails stay idempotent too) but hidden
/// (<see cref="InApp"/> is false).
/// </remarks>
public sealed class Notification : Entity
{
    public const int TypeMaxLength = 50;

    public const int LinkMaxLength = 500;

    public const int DedupeKeyMaxLength = 200;

    private Notification(
        Guid id,
        Guid userId,
        string type,
        string data,
        string link,
        string dedupeKey,
        bool inApp,
        DateTime createdOnUtc)
        : base(id)
    {
        UserId = userId;
        Type = type;
        Data = data;
        Link = link;
        DedupeKey = dedupeKey;
        InApp = inApp;
        CreatedOnUtc = createdOnUtc;
    }

    /// <remarks>
    /// Required by EF Core.
    /// </remarks>
    private Notification()
    {
    }

    public Guid UserId { get; private set; }

    public string Type { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the JSON object with the values the frontend needs to render the notification.
    /// </summary>
    public string Data { get; private set; } = "{}";

    /// <summary>
    /// Gets the relative frontend path the notification opens (e.g. <c>/trips/{bookingId}</c>).
    /// </summary>
    public string Link { get; private set; } = string.Empty;

    public string DedupeKey { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the notification is shown in the app (false when the user turned the category
    /// off when it was created).
    /// </summary>
    public bool InApp { get; private set; }

    public DateTime CreatedOnUtc { get; private set; }

    public DateTime? ReadOnUtc { get; private set; }

    public static Notification Create(
        Guid userId,
        string type,
        string data,
        string link,
        string dedupeKey,
        bool inApp,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(dedupeKey);

        return new Notification(Guid.NewGuid(), userId, type, data, link, dedupeKey, inApp, utcNow);
    }

    public void MarkRead(DateTime utcNow) => ReadOnUtc ??= utcNow;
}
