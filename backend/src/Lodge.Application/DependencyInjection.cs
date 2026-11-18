using System.Reflection;
using FluentValidation;
using Lodge.Application.Abstractions.App;
using Lodge.Application.Abstractions.Assets;
using Lodge.Application.Abstractions.Behaviors;
using Lodge.Application.Abstractions.Caching;
using Lodge.Application.Authentication;
using Lodge.Application.Apartments;
using Lodge.Application.Bookings.Complete;
using Lodge.Application.Bookings.Expire;
using Lodge.Application.Hosting;
using Lodge.Application.Hosting.Superhost;
using Lodge.Application.Stripe;
using Lodge.Application.Users.TwoFactor;
using Lodge.Domain.Bookings;
using Microsoft.Extensions.DependencyInjection;

namespace Lodge.Application;

/// <summary>
/// Registers the application layer services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        Assembly assembly = typeof(DependencyInjection).Assembly;

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // Behaviors run in registration order (outermost first). The unit of work wraps the idempotency check
        // so that idempotency records are rolled back together with failed commands.
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);
            config.AddOpenBehavior(typeof(RequestLoggingPipelineBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
            config.AddOpenBehavior(typeof(QueryCachingPipelineBehavior<,>));
            config.AddOpenBehavior(typeof(CacheInvalidationPipelineBehavior<,>));
            config.AddOpenBehavior(typeof(UnitOfWorkPipelineBehavior<,>));
            config.AddOpenBehavior(typeof(IdempotentCommandPipelineBehavior<,>));
        });

        services.AddScoped<CacheInvalidator>();
        services.AddScoped<ICacheInvalidator>(sp => sp.GetRequiredService<CacheInvalidator>());

        services.AddSingleton<PricingService>();

        services.AddOptions<AppOptions>()
            .BindConfiguration(AppOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IAccountTokenService, AccountTokenService>();
        services.AddScoped<TwoFactorCodeVerifier>();

        services.AddOptions<AssetsOptions>().BindConfiguration(AssetsOptions.SectionName);
        services.AddSingleton<IAssetUrlResolver, AssetUrlResolver>();

        services.AddOptions<StripeOptions>()
            .BindConfiguration(StripeOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<StripeClientProvider>();

        services.AddOptions<CompleteBookingsOptions>()
            .BindConfiguration(CompleteBookingsOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<ICompleteBookingsJob, CompleteBookingsJob>();

        services.AddOptions<ExpireBookingRequestsOptions>()
            .BindConfiguration(ExpireBookingRequestsOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<IExpireBookingRequestsJob, ExpireBookingRequestsJob>();

        services.AddOptions<SuperhostOptions>()
            .BindConfiguration(SuperhostOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<SuperhostJobOptions>()
            .BindConfiguration(SuperhostJobOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<IUpdateSuperhostStatusJob, UpdateSuperhostStatusJob>();

        services.AddSingleton<ApartmentReader>();
        services.AddScoped<IHostAccess, HostAccess>();

        return services;
    }
}
