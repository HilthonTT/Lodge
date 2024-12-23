﻿using Azure.Storage.Blobs;
using Hangfire;
using Hangfire.PostgreSql;
using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Caching;
using Lodge.Application.Abstractions.Cryptography;
using Lodge.Application.Abstractions.Emails;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Abstractions.Notifications;
using Lodge.Application.Abstractions.Storage;
using Lodge.Domain.Core.Guards;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;
using Lodge.Infrastructure.Authentication;
using Lodge.Infrastructure.Authentication.Settings;
using Lodge.Infrastructure.Caching;
using Lodge.Infrastructure.Caching.Settings;
using Lodge.Infrastructure.Cryptography;
using Lodge.Infrastructure.Emails;
using Lodge.Infrastructure.Emails.Settings;
using Lodge.Infrastructure.Messaging;
using Lodge.Infrastructure.Messaging.Settings;
using Lodge.Infrastructure.Notifications;
using Lodge.Infrastructure.Storage;
using Lodge.Infrastructure.Storage.Settings;
using Lodge.Infrastructure.Time;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Lodge.Infrastructure;

public static class DependencyInjection
{
    private const string DatabaseConnectionString = "Database";

    /// <summary>
    /// Registers the necessary services with the DI framework.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddTransient<IDateTimeProvider, DateTimeProvider>();

        AddAuthentication(services, configuration);
        AddCaching(services, configuration);
        AddMessaging(services, configuration);
        AddEmail(services, configuration);
        AddStorage(services, configuration);
        AddBackgroundJobs(services, configuration);
        AddHealthChecks(services, configuration);

        return services;
    }

    /// <summary>
    /// Registers the authentication services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    private static void AddAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SettingsKey));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                string? jwtSecurityKey = configuration["Jwt:SecurityKey"];

                Ensure.NotNullOrEmpty(jwtSecurityKey, nameof(jwtSecurityKey));

                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSecurityKey))
                };
            });

        services.AddAuthorization();

        services.AddTransient<IPasswordHasher, PasswordHasher>();

        services.AddTransient<IPasswordHashChecker, PasswordHasher>();

        services.AddScoped<IJwtProvider, JwtProvider>();

        services.AddScoped<IUserIdentifierProvider, UserIdentifierProvider>();
    }

    /// <summary>
    /// Registers the cache services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    private static void AddCaching(IServiceCollection services, IConfiguration configuration)
    {
        string? redisConnectionString = configuration.GetConnectionString(CacheSettings.SettingsKey);

        Ensure.NotNullOrEmpty(redisConnectionString, nameof(redisConnectionString));

        services.AddStackExchangeRedisCache(options =>
            options.Configuration = redisConnectionString);

        services.AddSingleton<ICacheService, CacheService>();
    }

    /// <summary>
    /// Registers the messaging services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    private static void AddMessaging(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MessageBrokerSettings>(configuration.GetSection(MessageBrokerSettings.SettingsKey));

        services.AddSingleton<IIntegrationEventPublisher, IntegrationEventPublisher>();
    }

    /// <summary>
    /// Registers the email services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    private static void AddEmail(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MailSettings>(configuration.GetSection(MailSettings.SettingsKey));

        services.AddTransient<IEmailService, EmailService>();

        services.AddTransient<IEmailNotificationService, EmailNotificationService>();
    }

    /// <summary>
    /// Registers the storage services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    private static void AddStorage(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BlobServiceSettings>(configuration.GetSection(BlobServiceSettings.SettingsKey));

        string? blobConnectionString = configuration.GetConnectionString(BlobServiceSettings.SettingsKey);

        Ensure.NotNullOrEmpty(blobConnectionString, nameof(blobConnectionString));

        services.AddSingleton<IBlobService, BlobService>();

        services.AddSingleton(_ => new BlobServiceClient(blobConnectionString));
    }
    
    /// <summary>
    /// Registers the hangfire background job.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    private static void AddBackgroundJobs(IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString(DatabaseConnectionString);

        Ensure.NotNullOrEmpty(connectionString, nameof(connectionString));

        services.AddHangfire(config =>
            config.UsePostgreSqlStorage(
                options => options.UseNpgsqlConnection(connectionString)));

        services.AddHangfireServer(options => options.SchedulePollingInterval = TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Registers the health checks.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    private static void AddHealthChecks(IServiceCollection services, IConfiguration configuration)
    {
        string? dbConnectionString = configuration.GetConnectionString(DatabaseConnectionString);
        string? redisConnectionString = configuration.GetConnectionString(CacheSettings.SettingsKey);
        string? blobConnectionString = configuration.GetConnectionString(BlobServiceSettings.SettingsKey);

        Ensure.NotNullOrEmpty(dbConnectionString, nameof(dbConnectionString));
        Ensure.NotNullOrEmpty(redisConnectionString, nameof(redisConnectionString));
        Ensure.NotNullOrEmpty(blobConnectionString, nameof(blobConnectionString));

        services
            .AddHealthChecks()
            .AddNpgSql(dbConnectionString)
            .AddRedis(redisConnectionString)
            .AddAzureBlobStorage(blobConnectionString)
            .AddHangfire(options =>
            {
                options.MinimumAvailableServers = 1;
                options.MaximumJobsFailed = 1;
            });
    }
}
