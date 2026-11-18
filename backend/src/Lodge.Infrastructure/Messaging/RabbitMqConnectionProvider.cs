using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Lodge.Infrastructure.Messaging;

/// <summary>
/// Owns the single, lazily created RabbitMQ connection of the process (connections are expensive,
/// channels are cheap). Connecting lazily keeps the API bootable while the broker is still starting.
/// </summary>
public sealed class RabbitMqConnectionProvider(IOptions<MessageBrokerOptions> options) : IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    public MessageBrokerOptions Options => options.Value;

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _lock.WaitAsync(cancellationToken);

        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
            }

            MessageBrokerOptions brokerOptions = options.Value;

            var factory = new ConnectionFactory
            {
                HostName = brokerOptions.HostName,
                Port = brokerOptions.Port,
                UserName = brokerOptions.UserName,
                Password = brokerOptions.Password,
                VirtualHost = brokerOptions.VirtualHost,
                ClientProvidedName = "lodge-api",
                AutomaticRecoveryEnabled = true,
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);

            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Opens a new channel and makes sure the (durable) integration event queue exists.
    /// </summary>
    public async Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken = default)
    {
        IConnection connection = await GetConnectionAsync(cancellationToken);

        IChannel channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: options.Value.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        return channel;
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _lock.Dispose();
    }
}
