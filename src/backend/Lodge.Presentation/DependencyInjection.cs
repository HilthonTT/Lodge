using Asp.Versioning;
using Lodge.Presentation.Extensions;
using Lodge.Presentation.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Lodge.Presentation;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the necessary services with the DI framework.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddEndpoints(Assembly.GetExecutingAssembly());

        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1);
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'V";
            options.SubstituteApiVersionInUrl = true;
        });

        services.ConfigureOptions<ConfigureSwaggeGenOptions>();

        return services;
    }
}
