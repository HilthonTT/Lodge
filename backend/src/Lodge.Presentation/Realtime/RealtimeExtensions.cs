using Lodge.Application.Abstractions.Realtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace Lodge.Presentation.Realtime;

/// <summary>
/// Registers and maps SignalR.
/// </summary>
public static class RealtimeExtensions
{
    /// <summary>
    /// The Redis channel prefix of the SignalR backplane.
    /// </summary>
    public const string BackplaneChannelPrefix = "lodge-signalr";

    /// <summary>
    /// Adds SignalR with the Redis backplane (connection string <c>ConnectionStrings:Cache</c>, resolved lazily), so
    /// that events reach users connected to any API instance.
    /// </summary>
    internal static IServiceCollection AddRealtime(this IServiceCollection services)
    {
        services.AddSignalR().AddStackExchangeRedis();

        services.AddOptions<RedisOptions>().Configure<IConfiguration>((options, configuration) =>
        {
            string? connectionString = configuration.GetConnectionString("Cache");

            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                options.Configuration = ConfigurationOptions.Parse(connectionString);
            }

            options.Configuration.ChannelPrefix = RedisChannel.Literal(BackplaneChannelPrefix);
            options.Configuration.AbortOnConnectFail = false;
        });

        services.AddSingleton<IUserIdProvider, UserIdClaimUserIdProvider>();

        services.RemoveAll<IRealtimeNotifier>();
        services.AddSingleton<IRealtimeNotifier, SignalRRealtimeNotifier>();

        return services;
    }

    /// <summary>
    /// Maps the hub at <c>/hubs/notifications</c> (outside the versioned <c>/api/v1</c> routes).
    /// </summary>
    public static IEndpointRouteBuilder MapRealtimeHubs(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<NotificationsHub>(NotificationsHub.Path).RequireAuthorization();

        return endpoints;
    }
}
