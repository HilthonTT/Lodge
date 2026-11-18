using Dapper;
using Lodge.Domain.Core.Events;
using Lodge.Domain.Core.Primitives;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Npgsql;

namespace Lodge.Persistence.Outbox;

/// <summary>
/// Dispatches the domain events stored in the outbox. Rows are locked with <c>FOR UPDATE SKIP LOCKED</c>, so
/// several API instances can run the job concurrently without processing a message twice.
/// </summary>
internal sealed partial class ProcessOutboxMessagesJob(
    NpgsqlDataSource dataSource,
    IPublisher publisher,
    IDateTimeProvider dateTimeProvider,
    IOptions<OutboxOptions> options,
    ILogger<ProcessOutboxMessagesJob> logger) : IProcessOutboxMessagesJob
{
    public async Task ProcessAsync()
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync();
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();

        IReadOnlyList<OutboxMessageResponse> outboxMessages = await GetOutboxMessagesAsync(connection, transaction);

        if (outboxMessages.Count == 0)
        {
            return;
        }

        foreach (OutboxMessageResponse outboxMessage in outboxMessages)
        {
            Exception? exception = null;

            try
            {
                IDomainEvent domainEvent = JsonConvert.DeserializeObject<IDomainEvent>(
                    outboxMessage.Content,
                    OutboxSerializer.Settings)!;

                await publisher.Publish(domainEvent);
            }
#pragma warning disable CA1031 // A failing message must not stop the batch; the error is stored on the row.
            catch (Exception caughtException)
#pragma warning restore CA1031
            {
                LogProcessingFailed(logger, caughtException, outboxMessage.Id);
                exception = caughtException;
            }

            await connection.ExecuteAsync(
                """
                UPDATE outbox_messages
                SET processed_on_utc = @ProcessedOnUtc,
                    error = @Error
                WHERE id = @Id
                """,
                new { outboxMessage.Id, ProcessedOnUtc = dateTimeProvider.UtcNow, Error = exception?.ToString() },
                transaction);
        }

        await transaction.CommitAsync();

        LogProcessed(logger, outboxMessages.Count);
    }

    private async Task<IReadOnlyList<OutboxMessageResponse>> GetOutboxMessagesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction)
    {
        IEnumerable<OutboxMessageResponse> outboxMessages = await connection.QueryAsync<OutboxMessageResponse>(
            """
            SELECT id, content
            FROM outbox_messages
            WHERE processed_on_utc IS NULL
            ORDER BY created_on_utc
            LIMIT @BatchSize
            FOR UPDATE SKIP LOCKED
            """,
            new { options.Value.BatchSize },
            transaction);

        return outboxMessages.ToList();
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Exception while processing outbox message {MessageId}")]
    private static partial void LogProcessingFailed(ILogger logger, Exception exception, Guid messageId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Processed {Count} outbox messages")]
    private static partial void LogProcessed(ILogger logger, int count);
}
