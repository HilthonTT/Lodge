using Lodge.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Api.Extensions;

public static class MigrationExtensions
{
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        using LodgeDbContext dbContext = 
            scope.ServiceProvider.GetRequiredService<LodgeDbContext>();

        dbContext.Database.Migrate();
    }
}
