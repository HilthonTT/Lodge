using Lodge.BackgroundTasks.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Lodge.BackgroundTasks;

/// <summary>
/// Registers the integration event consumer and its handlers.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddBackgroundTasks(this IServiceCollection services)
    {
        services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddHostedService<IntegrationEventConsumerBackgroundService>();

        return services;
    }
}
