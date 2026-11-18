using Newtonsoft.Json;

namespace Lodge.Persistence.Outbox;

/// <summary>
/// Serializer settings for outbox messages. Type names are embedded so the processor can restore the
/// concrete domain event; the payloads are written by this application only (never by clients).
/// </summary>
internal static class OutboxSerializer
{
    public static readonly JsonSerializerSettings Settings = new()
    {
        TypeNameHandling = TypeNameHandling.All,
    };
}
