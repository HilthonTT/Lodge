using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Lodge.Infrastructure.Messaging;

/// <summary>
/// Reports whether the RabbitMQ connection can be opened.
/// </summary>
internal sealed class RabbitMqHealthCheck(RabbitMqConnectionProvider connectionProvider) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await connectionProvider.GetConnectionAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
#pragma warning disable CA1031 // A health check must report, not throw.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: exception);
        }
    }
}
