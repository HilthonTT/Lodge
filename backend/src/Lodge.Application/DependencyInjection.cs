using System.Reflection;
using FluentValidation;
using Lodge.Application.Abstractions.App;
using Lodge.Application.Abstractions.Assets;
using Lodge.Application.Abstractions.Behaviors;
using Lodge.Application.Abstractions.Caching;
using Lodge.Application.Abstractions.Realtime;
using Lodge.Application.Authentication;
using Lodge.Application.Apartments;
using Lodge.Application.Bookings.Complete;
using Lodge.Application.Bookings.Expire;
using Lodge.Application.Experiences;
using Lodge.Application.Hosting;
using Lodge.Application.Hosting.Superhost;
using Lodge.Application.Messaging;
using Lodge.Application.Notifications;
using Lodge.Application.Payments;
using Lodge.Application.Payouts;
using Lodge.Application.Reviews.Publish;
using Lodge.Application.Stripe;
using Lodge.Application.Users.TwoFactor;
using Lodge.Domain.Bookings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

        services.AddOptions<PaymentsOptions>()
            .BindConfiguration(PaymentsOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(options => options.HasValidMode(), "Payments:Mode must be empty, Stripe or Simulated.")
            .ValidateOnStart();
        services.AddOptions<CurrencyOptions>()
            .BindConfiguration(CurrencyOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IPaymentModeProvider, PaymentModeProvider>();
        services.AddSingleton<BookingPricing>();
        services.AddSingleton<CurrencyConverter>();
        services.AddSingleton<StripePaymentGateway>();
        services.AddSingleton<SimulatedPaymentGateway>();
        services.AddSingleton<PaymentGateways>();
        services.AddScoped<BookingPayments>();
        services.AddScoped<IRefundProcessor, RefundProcessor>();

        services.AddOptions<PayoutJobOptions>()
            .BindConfiguration(PayoutJobOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<IPayDuePayoutsJob, PayDuePayoutsJob>();

        services.AddOptions<PublishReviewsJobOptions>()
            .BindConfiguration(PublishReviewsJobOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<IPublishReviewsJob, PublishReviewsJob>();

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

        // Messaging, notifications and realtime (the presentation layer replaces the null notifier with SignalR).
        services.TryAddSingleton<IRealtimeNotifier, NullRealtimeNotifier>();
        services.AddScoped<IRealtimeOutbox, RealtimeOutbox>();
        services.AddScoped<ConversationService>();
        services.AddScoped<INotificationPreferencesProvider, NotificationPreferencesProvider>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IUserNotifier, UserNotifier>();
        services.AddOptions<RemindersJobOptions>()
            .BindConfiguration(RemindersJobOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<ISendRemindersJob, SendRemindersJob>();

        // Experiences.
        services.AddScoped<ExperiencePayments>();
        services.AddOptions<ExperienceJobOptions>()
            .BindConfiguration(ExperienceJobOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<IExperienceJobs, ExperienceJobs>();

        return services;
    }
}
