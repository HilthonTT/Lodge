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

// CORS is intentionally handled by the API only: the gateway forwards preflight requests unchanged,
// and adding CORS here as well would produce duplicate Access-Control-* headers.
app.UseRateLimiter();

// The gateway's own liveness probe; /health is proxied to the API like every other path.
app.MapHealthChecks("/gateway/health");
app.MapReverseProxy();

await app.RunAsync();
