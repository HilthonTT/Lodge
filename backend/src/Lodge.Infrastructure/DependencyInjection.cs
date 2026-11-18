using Azure.Storage.Blobs;
using Hangfire;
using Hangfire.PostgreSql;
using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Caching;
using Lodge.Application.Abstractions.Cryptography;
using Lodge.Application.Abstractions.Emails;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Abstractions.Notifications;
using Lodge.Application.Abstractions.Storage;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;
using Lodge.Infrastructure.Authentication;
using Lodge.Infrastructure.Caching;
using Lodge.Infrastructure.Cryptography;
using Lodge.Infrastructure.Emails;
using Lodge.Infrastructure.Messaging;
using Lodge.Infrastructure.Notifications;
using Lodge.Infrastructure.Storage;
using Lodge.Infrastructure.Time;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lodge.Infrastructure;

/// <summary>
/// Registers the infrastructure services. Connection strings are resolved lazily from <see cref="IConfiguration"/>
/// so that hosts (and tests) can override them after the services have been registered.
/// </summary>
public static class DependencyInjection
{
    public const string DatabaseConnectionStringName = "Database";
    public const string CacheConnectionStringName = "Cache";
    public const string BlobStorageConnectionStringName = "BlobStorage";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services
            .AddAuthenticationInternal()
            .AddCachingInternal()
            .AddMessagingInternal()
            .AddEmailInternal()
            .AddStorageInternal()
            .AddBackgroundJobsInternal();

        services.AddHealthChecks()
            .AddRedis(sp => GetRequiredConnectionString(sp, CacheConnectionStringName), name: "redis")
            .AddAzureBlobStorage(sp => sp.GetRequiredService<BlobServiceClient>(), name: "blob-storage")
            .AddCheck<RabbitMqHealthCheck>("rabbitmq")
            .AddHangfire(options => options.MinimumAvailableServers = 1, name: "hangfire");

        return services;
    }

    internal static string GetRequiredConnectionString(IServiceProvider serviceProvider, string name)
    {
        string? connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString(name);

        return string.IsNullOrWhiteSpace(connectionString)
            ? throw new InvalidOperationException($"The connection string 'ConnectionStrings:{name}' is not configured.")
            : connectionString;
    }

    private static IServiceCollection AddAuthenticationInternal(this IServiceCollection services)
    {
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();
        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.Admin, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(nameof(UserRole.Admin)));

        services.AddSingleton<PasswordHasher>();
        services.AddSingleton<IPasswordHasher>(sp => sp.GetRequiredService<PasswordHasher>());
        services.AddSingleton<IPasswordHashChecker>(sp => sp.GetRequiredService<PasswordHasher>());
        services.AddSingleton<IJwtProvider, JwtProvider>();
        services.AddScoped<IUserIdentifierProvider, UserIdentifierProvider>();

        return services;
    }

    private static IServiceCollection AddCachingInternal(this IServiceCollection services)
    {
        services.AddStackExchangeRedisCache(_ => { });
        services.AddOptions<RedisCacheOptions>()
            .Configure<IServiceProvider>((options, sp) =>
            {
                options.Configuration = GetRequiredConnectionString(sp, CacheConnectionStringName);
                options.InstanceName = "lodge:";
            });

        services.AddSingleton<ICacheService, CacheService>();

        return services;
    }

    private static IServiceCollection AddMessagingInternal(this IServiceCollection services)
    {
        services.AddOptions<MessageBrokerOptions>()
            .BindConfiguration(MessageBrokerOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<RabbitMqConnectionProvider>();
        services.AddSingleton<IIntegrationEventPublisher, IntegrationEventPublisher>();

        return services;
    }

    private static IServiceCollection AddEmailInternal(this IServiceCollection services)
    {
        services.AddOptions<MailOptions>().BindConfiguration(MailOptions.SectionName);

        services.AddTransient<IEmailService, EmailService>();
        services.AddTransient<IEmailNotificationService, EmailNotificationService>();

        return services;
    }

    private static IServiceCollection AddStorageInternal(this IServiceCollection services)
    {
        services.AddOptions<BlobStorageOptions>()
            .BindConfiguration(BlobStorageOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(sp => new BlobServiceClient(GetRequiredConnectionString(sp, BlobStorageConnectionStringName)));
        services.AddSingleton<IBlobService, BlobService>();

        return services;
    }

    private static IServiceCollection AddBackgroundJobsInternal(this IServiceCollection services)
    {
        services.AddHangfire((sp, config) => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options =>
                options.UseNpgsqlConnection(GetRequiredConnectionString(sp, DatabaseConnectionStringName))));

        services.AddHangfireServer(options => options.SchedulePollingInterval = TimeSpan.FromSeconds(1));

        return services;
    }
}
