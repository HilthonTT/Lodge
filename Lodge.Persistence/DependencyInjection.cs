using Lodge.Application.Abstractions.Data;
using Lodge.Domain.Core.Guards;
using Lodge.Persistence.Infrastructure;
using Lodge.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lodge.Persistence;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the necessary services with the DI framework.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<UpdateAuditableEntitiesInterceptor>();
        services.AddSingleton<SoftDeleteEntitiesInterceptor>();

        string? connectionString = configuration.GetConnectionString(ConnectionString.SettingsKey);

        Ensure.NotNullOrEmpty(connectionString, nameof(connectionString));

        services.AddDbContext<LodgeDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName);
            })
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(
                sp.GetRequiredService<UpdateAuditableEntitiesInterceptor>(),
                sp.GetRequiredService<SoftDeleteEntitiesInterceptor>());
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<LodgeDbContext>());

        return services;
    }
}
