namespace Lodge.Api.Extensions;

/// <summary>
/// Configures CORS for the browser frontend.
/// </summary>
internal static class CorsExtensions
{
    private const string AllowedOriginsKey = "Cors:AllowedOrigins";

    public static IServiceCollection AddFrontendCors(this IServiceCollection services, IConfiguration configuration)
    {
        string[] allowedOrigins = configuration.GetSection(AllowedOriginsKey).Get<string[]>() ?? [];

        if (allowedOrigins.Length == 0)
        {
            allowedOrigins = ["http://localhost:3000"];
        }

        services.AddCors(options => options.AddDefaultPolicy(policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithExposedHeaders("api-supported-versions", "X-Correlation-Id")));

        return services;
    }
}
