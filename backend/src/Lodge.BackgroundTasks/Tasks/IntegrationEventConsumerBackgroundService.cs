using Lodge.Application.Abstractions.Messaging;
using Lodge.Infrastructure.Messaging;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Lodge.BackgroundTasks.Tasks;

/// <summary>
/// Consumes integration events from RabbitMQ and dispatches them to the MediatR integration event handlers.
/// </summary>
/// <remarks>
/// Messages are acknowledged only after all handlers succeeded. A failing message is redelivered once and
/// dropped (logged) when it fails again, so a poison message cannot block the queue.
/// </remarks>
internal sealed partial class IntegrationEventConsumerBackgroundService(
    RabbitMqConnectionProvider connectionProvider,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<IntegrationEventConsumerBackgroundService> logger) : BackgroundService
{
    private const ushort PrefetchCount = 10;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IChannel? channel = null;

        while (channel is null && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                channel = await connectionProvider.CreateChannelAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogBrokerUnavailable(logger, exception, RetryDelay.TotalSeconds);
                await Task.Delay(RetryDelay, stoppingToken);
            }
        }

        if (channel is null)
        {
            return;
        }

        await using (channel)
        {
            await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: PrefetchCount, global: false, stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += (_, eventArgs) => OnMessageReceivedAsync(channel, eventArgs, stoppingToken);

            await channel.BasicConsumeAsync(
                connectionProvider.Options.QueueName,
                autoAck: false,
                consumer,
                stoppingToken);

            LogConsuming(logger, connectionProvider.Options.QueueName);

            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Host is shutting down.
            }
        }
    }

    private async Task OnMessageReceivedAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        CancellationToken cancellationToken)
    {
        IIntegrationEvent? integrationEvent =
            IntegrationEventSerializer.Deserialize(eventArgs.BasicProperties.Type, eventArgs.Body.Span);

        if (integrationEvent is null)
        {
            LogUnknownMessage(logger, eventArgs.BasicProperties.Type);
            await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false, cancellationToken);
            return;
        }

        try
        {
            using IServiceScope scope = serviceScopeFactory.CreateScope();

            IPublisher publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            await publisher.Publish(integrationEvent, cancellationToken);

            await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogHandlingFailed(logger, exception, integrationEvent.GetType().Name, eventArgs.Redelivered);

            await channel.BasicNackAsync(
                eventArgs.DeliveryTag,
                multiple: false,
                requeue: !eventArgs.Redelivered,
                cancellationToken);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Message broker unavailable, retrying in {DelaySeconds}s")]
    private static partial void LogBrokerUnavailable(ILogger logger, Exception exception, double delaySeconds);

    [LoggerMessage(Level = LogLevel.Information, Message = "Consuming integration events from queue {QueueName}")]
    private static partial void LogConsuming(ILogger logger, string queueName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Dropping message with unknown integration event type {Type}")]
    private static partial void LogUnknownMessage(ILogger logger, string? type);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to handle integration event {EventType} (redelivered: {Redelivered})")]
    private static partial void LogHandlingFailed(ILogger logger, Exception exception, string eventType, bool redelivered);
}
