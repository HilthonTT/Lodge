using Lodge.Application.Abstractions.Behaviors;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using System.Reflection;
using Lodge.Domain.Bookings;
using Stripe;
using Microsoft.Extensions.Configuration;

namespace Lodge.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the necessary with the DI framework.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        Assembly assembly = typeof(DependencyInjection).Assembly;

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);

            config.AddOpenBehavior(typeof(ExceptionHandlingPipelineBehavior<,>));
            config.AddOpenBehavior(typeof(RequestLoggingPipelineBehavior<,>));
            config.AddOpenBehavior(typeof(IdempotentCommandPipelineBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
            config.AddOpenBehavior(typeof(UnitOfWorkPipelineBehavior<,>));
            config.AddOpenBehavior(typeof(QueryCachingPipelineBehavior<,>));
        });

        services.AddTransient<PricingService>();

        StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];

        return services;
    }
}
