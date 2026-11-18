using Lodge.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Api.Extensions;

/// <summary>
/// Contains extensions methods for EFCore migrations.
/// </summary>
public static class MigrationExtensions
{
    /// <summary>
    /// Applies the database context's migrations.
    /// </summary>
    /// <param name="app">The app.</param>
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        using LodgeDbContext dbContext = 
            scope.ServiceProvider.GetRequiredService<LodgeDbContext>();

        dbContext.Database.Migrate();
    }
}
