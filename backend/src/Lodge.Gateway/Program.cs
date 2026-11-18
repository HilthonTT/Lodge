using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Per-client-IP fixed window. Note that server-side rendering (Next.js) calls the gateway from a single IP,
// so the limit must be generous enough for that client.
int permitLimit = builder.Configuration.GetValue("RateLimiting:PermitLimitPerMinute", 600);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("per-ip", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

builder.Services.AddHealthChecks();

WebApplication app = builder.Build();

// SignalR (/hubs/**) has its own route and cluster (appsettings.json): WebSockets are proxied as-is, the route is
// exempt from rate limiting ("RateLimiterPolicy": "Disable": a connection is long-lived, and long polling would burn
// through the per-IP budget), and cookie-based session affinity keeps the negotiate and connect requests of a client on
// the same API instance (the Redis backplane fans events out across instances).
// CORS is intentionally handled by the API only: the gateway forwards preflight requests unchanged,
// and adding CORS here as well would produce duplicate Access-Control-* headers.
app.UseRateLimiter();

// The gateway's own liveness probe; /health is proxied to the API like every other path.
app.MapHealthChecks("/gateway/health");
app.MapReverseProxy();

await app.RunAsync();
