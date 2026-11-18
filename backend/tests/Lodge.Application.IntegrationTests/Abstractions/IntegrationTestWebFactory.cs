using Hangfire;
using Lodge.Application.Abstractions.Authentication;
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

namespace Lodge.Application.IntegrationTests.Abstractions;

/// <summary>
/// Hosts the API against throw-away PostgreSQL and Redis containers (requires a running Docker engine).
/// The Hangfire server and the RabbitMQ consumer are not started.
/// </summary>
public sealed class IntegrationTestWebFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("lodge")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:8-alpine").Build();

    public TestUserIdentifierProvider CurrentUser { get; } = new();

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
                // Never talk to a local SMTP server: e-mails are only logged.
                ["Mail:SmtpServer"] = "",
                ["Serilog:WriteTo:1:Name"] = "Console",
            }));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IUserIdentifierProvider>();
            services.AddSingleton<IUserIdentifierProvider>(CurrentUser);

            RemoveBackgroundProcessing(services);
        });
    }

    internal static void RemoveBackgroundProcessing(IServiceCollection services)
    {
        System.Reflection.Assembly hangfireAspNetCore = typeof(HangfireServiceCollectionExtensions).Assembly;

        List<ServiceDescriptor> hostedServices = services
            .Where(descriptor => descriptor.ServiceType == typeof(IHostedService) &&
                                 (descriptor.ImplementationType == typeof(IntegrationEventConsumerBackgroundService) ||
                                  descriptor.ImplementationFactory?.Method.DeclaringType?.Assembly == hangfireAspNetCore))
            .ToList();

        foreach (ServiceDescriptor descriptor in hostedServices)
        {
            services.Remove(descriptor);
        }
    }
}
