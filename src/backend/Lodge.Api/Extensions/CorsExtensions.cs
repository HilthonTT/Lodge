using Lodge.Domain.Core.Guards;

namespace Lodge.Api.Extensions;

/// <summary>
/// Contains all the Cors extension methods.
/// </summary>
public static class CorsExtensions
{
    /// <summary>
    /// Adds the cors policies into the API.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddFrontendCors(this IServiceCollection services, IConfiguration configuration)
    {
        string? frontendUrl = configuration["AllowedOrigin"];

        Ensure.NotNullOrEmpty(frontendUrl, nameof(frontendUrl));

        services.AddCors(options =>
        {
            options.AddPolicy("AllowSpecificOrigin", policy =>
            {
                policy
                    .WithOrigins(frontendUrl)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .Build();
            });
        });

        return services;
    }
}
