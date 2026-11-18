using System.Collections.Frozen;
using System.Text.Json;
using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Infrastructure.Messaging;

/// <summary>
/// Serializes integration events as plain JSON. The event type travels in the AMQP <c>type</c> property and
/// is resolved against an allow-list of known <see cref="IIntegrationEvent"/> types, so message payloads can
/// never instantiate arbitrary .NET types.
/// </summary>
public static class IntegrationEventSerializer
{
    private static readonly FrozenDictionary<string, Type> KnownTypes = typeof(IIntegrationEvent).Assembly
        .GetTypes()
        .Where(type => type is { IsClass: true, IsAbstract: false } && type.IsAssignableTo(typeof(IIntegrationEvent)))
        .ToFrozenDictionary(type => type.FullName!, StringComparer.Ordinal);

    public static string GetTypeName(IIntegrationEvent integrationEvent) => integrationEvent.GetType().FullName!;

    public static byte[] Serialize(IIntegrationEvent integrationEvent) =>
        JsonSerializer.SerializeToUtf8Bytes(integrationEvent, integrationEvent.GetType());

    public static IIntegrationEvent? Deserialize(string? typeName, ReadOnlySpan<byte> body)
    {
        if (typeName is null || !KnownTypes.TryGetValue(typeName, out Type? type))
        {
            return null;
        }

        return JsonSerializer.Deserialize(body, type) as IIntegrationEvent;
    }
}
