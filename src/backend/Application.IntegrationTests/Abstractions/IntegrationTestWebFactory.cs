using Hangfire;
using Hangfire.PostgreSql;
using Lodge.Application.Abstractions.Data;
using Lodge.BackgroundTasks.Tasks;
using Lodge.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

namespace Application.IntegrationTests.Abstractions;

public class IntegrationTestWebFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:latest")
        .WithDatabase("runtrackr")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7.0")
        .Build();

    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
        .WithImage("rabbitmq:management")
        .WithHostname("lodge-mq")
        .WithUsername("guest")
        .WithPassword("guest")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.AddHangfire(config =>
                config.UsePostgreSqlStorage(
                    options => options.UseNpgsqlConnection(_dbContainer.GetConnectionString())));

            services.RemoveAll(typeof(IDbConnectionFactory));
            services.TryAddSingleton<IDbConnectionFactory>(_ =>
                new DbConnectionFactory(new NpgsqlDataSourceBuilder(_dbContainer.GetConnectionString()).Build()));

            services.RemoveAll(typeof(DbContextOptions<LodgeDbContext>));
            services.AddDbContext<LodgeDbContext>(options =>
                options
                    .UseNpgsql(_dbContainer.GetConnectionString())
                    .UseSnakeCaseNamingConvention());

            services.RemoveAll(typeof(RedisCacheOptions));
            services.AddStackExchangeRedisCache(redisCacheOptions =>
                redisCacheOptions.Configuration = _redisContainer.GetConnectionString());

            List<ServiceDescriptor> hostedServices = services
                .Where(service => typeof(IHostedService).IsAssignableFrom(service.ServiceType))
                .ToList();

            foreach (var hostedService in hostedServices)
            {
                services.Remove(hostedService);
            }
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _redisContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _redisContainer.StopAsync();
        await _rabbitMqContainer.StopAsync();
    }
}
