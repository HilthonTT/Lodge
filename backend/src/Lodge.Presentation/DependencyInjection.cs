using System.Reflection;
using Asp.Versioning;
using Lodge.Presentation.Extensions;
using Lodge.Presentation.Infrastructure;
using Lodge.Presentation.OpenApi;
using Microsoft.Extensions.DependencyInjection;

namespace Lodge.Presentation;

/// <summary>
/// Registers the presentation layer (endpoints, API versioning, OpenAPI, error handling).
/// </summary>
public static class DependencyInjection
{
    public const string OpenApiDocumentName = "v1";

    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddEndpoints(Assembly.GetExecutingAssembly());

        services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1);
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
                options.ReportApiVersions = true;
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'V";
                options.SubstituteApiVersionInUrl = true;
            });

        // The versioned API explorer puts every v1 endpoint in the "v1" group, which is exactly the
        // document name below, so the built-in generator renders the substituted /api/v1/... routes.
        services.AddOpenApi(OpenApiDocumentName, options =>
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }
}
