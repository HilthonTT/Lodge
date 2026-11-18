using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Idempotency;
using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Experiences;
using Lodge.Domain.Messaging;
using Lodge.Domain.Payouts;
using Lodge.Domain.Reviews;
using Lodge.Domain.Users;
using Lodge.Domain.Wishlists;
using Lodge.Persistence.Idempotency;
using Lodge.Persistence.Interceptors;
using Lodge.Persistence.Outbox;
using Lodge.Persistence.Repositories;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Lodge.Persistence;

/// <summary>
/// Registers EF Core, Dapper and the repositories. The connection string (<c>ConnectionStrings:Database</c>)
/// is resolved lazily so hosts and tests can override it.
/// </summary>
public static class DependencyInjection
{
    public const string ConnectionStringName = "Database";

    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        services.AddSingleton<UpdateAuditableEntitiesInterceptor>();
        services.AddSingleton<SoftDeleteEntitiesInterceptor>();
        services.AddSingleton<InsertOutboxMessagesInterceptor>();

        // One data source (connection pool) shared by EF Core, Dapper and the health check.
        services.AddSingleton(sp =>
        {
            string? connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStringName);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"The connection string 'ConnectionStrings:{ConnectionStringName}' is not configured.");
            }

            return new NpgsqlDataSourceBuilder(connectionString).Build();
        });

        services.AddDbContext<LodgeDbContext>((sp, options) => options
            .UseNpgsql(sp.GetRequiredService<NpgsqlDataSource>())
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(
                sp.GetRequiredService<UpdateAuditableEntitiesInterceptor>(),
                sp.GetRequiredService<SoftDeleteEntitiesInterceptor>(),
                sp.GetRequiredService<InsertOutboxMessagesInterceptor>()));

        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<LodgeDbContext>());
        services.AddScoped<IDbContext>(sp => sp.GetRequiredService<LodgeDbContext>());

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserTokenRepository, UserTokenRepository>();
        services.AddScoped<IExternalLoginRepository, ExternalLoginRepository>();
        services.AddScoped<IRecoveryCodeRepository, RecoveryCodeRepository>();
        services.AddScoped<IApartmentRepository, ApartmentRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IPayoutRepository, PayoutRepository>();
        services.AddScoped<ICalendarDayRepository, CalendarDayRepository>();
        services.AddScoped<IWishlistRepository, WishlistRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IExperienceRepository, ExperienceRepository>();
        services.AddScoped<IIdempotencyService, IdempotencyService>();

        services.AddOptions<OutboxOptions>()
            .BindConfiguration(OutboxOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<IProcessOutboxMessagesJob, ProcessOutboxMessagesJob>();

        services.AddHealthChecks().AddNpgSql(name: "postgres");

        // Data Protection keys (used to encrypt TOTP secrets and the social login correlation cookie) are stored
        // in PostgreSQL, so they survive restarts and are shared by every API instance.
        services.AddDataProtection()
            .SetApplicationName("Lodge")
            .PersistKeysToDbContext<LodgeDbContext>();

        return services;
    }
}
