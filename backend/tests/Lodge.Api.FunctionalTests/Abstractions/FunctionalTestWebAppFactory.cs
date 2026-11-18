using Hangfire;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Abstractions.Notifications;
using Lodge.Application.Abstractions.Storage;
using Lodge.BackgroundTasks.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Lodge.Api.FunctionalTests.Abstractions;

/// <summary>
/// Hosts the full HTTP API against throw-away PostgreSQL and Redis containers (requires a running Docker engine).
/// Demo data is seeded, so tests can sign in as the demo user. E-mails are recorded, files are kept in memory and
/// integration events are dispatched in-process (no RabbitMQ). Payments run in simulated mode (Development without
/// Stripe keys). Background jobs (outbox, payouts, ...) do not run on their own: tests trigger them.
/// </summary>
public sealed class FunctionalTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string PublicBaseUrl = "http://lodge.test";
    public const string FrontendBaseUrl = "http://frontend.test";

    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("lodge")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:8-alpine").Build();

    public RecordingEmailNotificationService Emails { get; } = new();

    public InMemoryBlobService Blobs { get; } = new();

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_dbContainer.StartAsync(), _redisContainer.StartAsync());
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _dbContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = _dbContainer.GetConnectionString(),
                ["ConnectionStrings:Cache"] = _redisContainer.GetConnectionString(),
                ["Database:ApplyMigrationsOnStartup"] = "true",
                ["Database:SeedDemoData"] = "true",
                ["Assets:PublicBaseUrl"] = PublicBaseUrl,
                ["App:ApiPublicBaseUrl"] = PublicBaseUrl,
                ["App:FrontendBaseUrl"] = FrontendBaseUrl,
                ["Mail:SmtpServer"] = "",
                ["BackgroundJobs:Outbox:BatchSize"] = "1000",
                ["Serilog:WriteTo:1:Name"] = "Console",
            }));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailNotificationService>();
            services.AddSingleton<IEmailNotificationService>(Emails);

            services.RemoveAll<IBlobService>();
            services.AddSingleton<IBlobService>(Blobs);

            services.RemoveAll<IIntegrationEventPublisher>();
            services.AddSingleton<IIntegrationEventPublisher, InProcessIntegrationEventPublisher>();

            System.Reflection.Assembly hangfireAspNetCore = typeof(HangfireServiceCollectionExtensions).Assembly;

            List<ServiceDescriptor> backgroundServices = services
                .Where(descriptor => descriptor.ServiceType == typeof(IHostedService) &&
                                     (descriptor.ImplementationType == typeof(IntegrationEventConsumerBackgroundService) ||
                                      descriptor.ImplementationFactory?.Method.DeclaringType?.Assembly == hangfireAspNetCore))
                .ToList();

            foreach (ServiceDescriptor descriptor in backgroundServices)
            {
                services.Remove(descriptor);
            }
        });
    }
}
