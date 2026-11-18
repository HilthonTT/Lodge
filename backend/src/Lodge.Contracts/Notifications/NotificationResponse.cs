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
