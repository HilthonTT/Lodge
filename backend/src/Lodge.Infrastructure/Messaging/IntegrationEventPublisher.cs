using Lodge.Application.Abstractions.Messaging;
using Lodge.Infrastructure.Messaging.Settings;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace Lodge.Infrastructure.Messaging;

/// <summary>
/// Represents the integration event publisher.
/// </summary>
/// <param name="options">The message broker settings options.</param>
internal sealed class IntegrationEventPublisher : IIntegrationEventPublisher, IDisposable
{
    private static readonly JsonSerializerSettings JsonSerializerSettings = new()
    {
        TypeNameHandling = TypeNameHandling.Auto,
    };

    private readonly MessageBrokerSettings _messageBrokerSettings;
    private readonly IConnection _connection;
    private readonly IModel _channel;

    /// <summary>
    /// Initializes a new instance of <see cref="IntegrationEventPublisher"/> class.
    /// </summary>
    /// <param name="messageBrokerSettings"></param>
    public IntegrationEventPublisher(IOptions<MessageBrokerSettings> messageBrokerSettings)
    {
        _messageBrokerSettings = messageBrokerSettings.Value;

        ConnectionFactory factory = new()
        {
            HostName = _messageBrokerSettings.HostName,
            Port = _messageBrokerSettings.Port,
            UserName = _messageBrokerSettings.UserName,
            Password = _messageBrokerSettings.Password,
        };

        _connection = factory.CreateConnection();

        _channel = _connection.CreateModel();

        _channel.QueueDeclare(_messageBrokerSettings.QueueName, false, false, false);
    }

    public void Publish(IIntegrationEvent integrationEvent)
    {
        string payload = JsonConvert.SerializeObject(
            integrationEvent, 
            typeof(IIntegrationEvent), 
            JsonSerializerSettings);

        byte[] body = Encoding.UTF8.GetBytes(payload);

        _channel.BasicPublish(string.Empty, _messageBrokerSettings.QueueName, body: body);
    }

    public void Dispose()
    {
        _connection?.Dispose();

        _channel?.Dispose();
    }
}
