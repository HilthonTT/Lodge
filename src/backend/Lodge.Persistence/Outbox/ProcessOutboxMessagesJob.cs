using Lodge.Application.Abstractions.Data;
using Lodge.Domain.Core.Primitives;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data;
using Dapper;
using Lodge.Domain.Core.Events;

namespace Lodge.Persistence.Outbox;

/// <summary>
/// Represents the process outbox messages job.
/// </summary>
/// <param name="factory">The database connection factory.</param>
/// <param name="publisher">The MediatR publisher.</param>
/// <param name="dateTimeProvider">The date time provider.</param>
/// <param name="logger">The logger.</param>
internal sealed class ProcessOutboxMessagesJob(
    IDbConnectionFactory factory, 
    IPublisher publisher, 
    IDateTimeProvider dateTimeProvider,
    ILogger<ProcessOutboxMessagesJob> logger) : IProcessOutboxMessagesJob
{
    private const int BatchSize = 15;
    private static readonly JsonSerializerSettings SerializerSettings = new()
    {
        TypeNameHandling = TypeNameHandling.All
    };

    /// <inheritdoc />
    public async Task ProcessAsync()
    {
        logger.LogInformation("Beginning to process outbox messages");

        using IDbConnection connection = await factory.GetOpenConnectionAsync();
        using IDbTransaction transaction = connection.BeginTransaction();

        IReadOnlyList<OutboxMessageResponse> outboxMessages = await GetOutboxMessagesAsync(connection, transaction);

        if (!outboxMessages.Any())
        {
            logger.LogInformation("Completed processing outbox messages - no messages to process");

            return;
        }

        foreach (OutboxMessageResponse outboxMessage in outboxMessages)
        {
            Exception? exception = null;

            try
            {
                IDomainEvent domainEvent = JsonConvert.DeserializeObject<IDomainEvent>(
                    outboxMessage.Content,
                    SerializerSettings)!;

                await publisher.Publish(domainEvent);
            }
            catch (Exception caughtException)
            {
                logger.LogError(
                    caughtException,
                    "Exception while processing outbox message {MessageId}",
                    outboxMessage.Id);

                exception = caughtException;
            }

            await UpdateOutboxMessageAsync(connection, transaction, outboxMessage, exception);
        }

        transaction.Commit();

        logger.LogInformation("Completed processing outbox messages");
    }

    /// <summary>
    /// Gets the outbox messages that haven't yet been processed.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="transaction">The database transaction.</param>
    /// <returns>The readonly list of <see cref="OutboxMessageResponse"/> records.</returns>
    private static async Task<IReadOnlyList<OutboxMessageResponse>> GetOutboxMessagesAsync(
        IDbConnection connection,
        IDbTransaction transaction)
    {
        const string sql =
            """
            SELECT 
                id,
                content
            FROM outbox_messages
            WHERE processed_on_utc IS NULL
            ORDER BY created_on_utc
            LIMIT @BatchSize
            FOR UPDATE SKIP LOCKED
            """;

        IEnumerable<OutboxMessageResponse> outboxMessages = await connection.QueryAsync<OutboxMessageResponse>(
            sql,
            new { BatchSize },
            transaction);

        return outboxMessages.ToList();
    }

    /// <summary>
    /// Updates the outbox message, "processes" it.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="transaction">The database transaction.</param>
    /// <param name="outboxMessage">The outbox message.</param>
    /// <param name="exception">The exception.</param>
    /// <returns>The completed task.</returns>
    private async Task UpdateOutboxMessageAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        OutboxMessageResponse outboxMessage,
        Exception? exception)
    {
        const string sql =
            """
            UPDATE outbox_messages
            SET processed_on_utc = @ProcessedOnUtc,
                error = @Error
            WHERE id = @Id
            """;

        await connection.ExecuteAsync(
            sql,
            new
            {
                outboxMessage.Id,
                ProcessedOnUtc = dateTimeProvider.UtcNow,
                Error = exception?.ToString()
            },
            transaction);
    }
}
