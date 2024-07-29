using Lodge.Application.Abstractions.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace Lodge.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the necessary with the DI framework.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);

            config.AddOpenBehavior(typeof(UnitOfWorkPipelineBehavior<,>));
        });

        return services;
    }
}
