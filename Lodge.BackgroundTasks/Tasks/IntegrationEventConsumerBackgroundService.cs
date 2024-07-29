using Lodge.Application.Abstractions.Messaging;
using Lodge.BackgroundTasks.Services;
using Lodge.Infrastructure.Messaging.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Lodge.BackgroundTasks.Tasks;

/// <summary>
/// Represents the integration event consumer background service.
/// </summary>
internal sealed class IntegrationEventConsumerBackgroundService: IHostedService, IDisposable
{
    private static readonly JsonSerializerSettings SerializerSettings = new()
    {
        TypeNameHandling = TypeNameHandling.All
    };

    private readonly IServiceProvider _serviceProvider;
    private readonly IModel _channel;
    private readonly IConnection _connection;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationEventConsumerBackgroundService"/>
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="options">The message broker settings options.</param>
    /// <param name="serviceProvider">The service provider.</param>
    public IntegrationEventConsumerBackgroundService(
        ILogger<IntegrationEventConsumerBackgroundService> logger,
        IOptions<MessageBrokerSettings> options,
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        MessageBrokerSettings messageBrokerSettings = options.Value;

        var factory = new ConnectionFactory
        {
            HostName = messageBrokerSettings.HostName,
            Port = messageBrokerSettings.Port,
            UserName = messageBrokerSettings.UserName,
            Password = messageBrokerSettings.Password
        };

        _connection = factory.CreateConnection();

        _channel = _connection.CreateModel();

        _channel.QueueDeclare(messageBrokerSettings.QueueName, false, false, false);

        try
        {
            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += OnIntegrationEventReceived;

            _channel.BasicConsume(messageBrokerSettings.QueueName, false, consumer);
        }
        catch (Exception e)
        {
            logger.LogCritical("ERROR: Failed to process the integration events: {Message}", e.Message);
        }
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        Dispose();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }

    /// <summary>
    /// Processes the integration event received from the message queue.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="eventArgs">The event arguments.</param>
    private void OnIntegrationEventReceived(object? sender, BasicDeliverEventArgs eventArgs)
    {
        string body = Encoding.UTF8.GetString(eventArgs.Body.Span);

        IIntegrationEvent integrationEvent = JsonConvert.DeserializeObject<IIntegrationEvent>(
            body, 
            SerializerSettings)!;

        using IServiceScope scope = _serviceProvider.CreateScope();

        IIntegrationEventConsumer integrationEventConsumer = 
            scope.ServiceProvider.GetRequiredService<IIntegrationEventConsumer>();

        integrationEventConsumer.Consume(integrationEvent);

        _channel.BasicAck(eventArgs.DeliveryTag, false);
    }
}
