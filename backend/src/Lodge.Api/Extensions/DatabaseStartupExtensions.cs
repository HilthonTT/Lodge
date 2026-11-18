using Lodge.Api.Seeding;
using Lodge.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lodge.Api.Extensions;

internal static partial class DatabaseStartupExtensions
{
    public static IServiceCollection AddDatabaseStartup(this IServiceCollection services)
    {
        services.AddOptions<DatabaseStartupOptions>().BindConfiguration(DatabaseStartupOptions.SectionName);
        services.AddScoped<DemoDataSeeder>();

        return services;
    }

    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        DatabaseStartupOptions options = app.Services.GetRequiredService<IOptions<DatabaseStartupOptions>>().Value;

        if (!options.ApplyMigrationsOnStartup && !options.SeedDemoData)
        {
            return;
        }

        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();

        if (options.ApplyMigrationsOnStartup)
        {
            LodgeDbContext dbContext = scope.ServiceProvider.GetRequiredService<LodgeDbContext>();

            LogApplyingMigrations(app.Logger);
            await dbContext.Database.MigrateAsync();
        }

        if (options.SeedDemoData)
        {
            await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync();
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Applying database migrations")]
    private static partial void LogApplyingMigrations(ILogger logger);
}
