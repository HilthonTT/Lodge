using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Configuration;

namespace Lodge.Api.FunctionalTests.Gateway;

/// <summary>
/// Validates the gateway's YARP configuration (<c>backend/src/Lodge.Gateway/appsettings.json</c>) with YARP's own
/// validator, and checks the SignalR route: WebSockets go through <c>/hubs/**</c> on a cluster with session affinity,
/// without rate limiting.
/// </summary>
public sealed class GatewayConfigurationTests
{
    [Fact]
    public async Task GatewayConfiguration_Should_BeValid_AndProxyTheHubs()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile(GatewaySettingsPath(), optional: false)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRouting();
        services.AddRateLimiter(options => options.AddPolicy(
            "per-ip",
            _ => RateLimitPartition.GetNoLimiter("test")));
        services.AddReverseProxy().LoadFromConfig(configuration.GetSection("ReverseProxy"));

        await using ServiceProvider provider = services.BuildServiceProvider();

        IProxyConfig proxyConfig = provider.GetRequiredService<IProxyConfigProvider>().GetConfig();
        IConfigValidator validator = provider.GetRequiredService<IConfigValidator>();

        foreach (RouteConfig route in proxyConfig.Routes)
        {
            (await validator.ValidateRouteAsync(route)).Should().BeEmpty(route.RouteId);
        }

        foreach (ClusterConfig cluster in proxyConfig.Clusters)
        {
            (await validator.ValidateClusterAsync(cluster)).Should().BeEmpty(cluster.ClusterId);
        }

        RouteConfig hubs = proxyConfig.Routes.Single(route => route.RouteId == "lodge-hubs");
        hubs.Match.Path.Should().Be("/hubs/{**catch-all}");
        hubs.RateLimiterPolicy.Should().Be("Disable");
        hubs.Order.Should().BeLessThan(proxyConfig.Routes.Single(route => route.RouteId == "lodge-api").Order ?? 0);

        ClusterConfig hubCluster = proxyConfig.Clusters.Single(cluster => cluster.ClusterId == hubs.ClusterId);
        hubCluster.SessionAffinity!.Enabled.Should().BeTrue();
        hubCluster.Destinations.Should().NotBeEmpty();
    }

    private static string GatewaySettingsPath()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lodge.slnx")))
        {
            directory = directory.Parent;
        }

        return directory is null
            ? throw new InvalidOperationException("The repository root (Lodge.slnx) was not found.")
            : Path.Combine(directory.FullName, "backend", "src", "Lodge.Gateway", "appsettings.json");
    }
}
