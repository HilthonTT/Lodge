using HealthChecks.UI.Client;
using Lodge.Api.Middlewares;
using Lodge.Presentation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Net.Http.Headers;
using Scalar.AspNetCore;

namespace Lodge.Api.Extensions;

/// <summary>
/// Middleware and endpoint registrations of the API host.
/// </summary>
internal static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseRequestContextLogging(this IApplicationBuilder app) =>
        app.UseMiddleware<RequestContextLoggingMiddleware>();

    /// <summary>
    /// Serves <c>wwwroot</c> (apartment images under <c>/images</c>) with a one-day cache lifetime.
    /// </summary>
    public static IApplicationBuilder UseApartmentImages(this IApplicationBuilder app) =>
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = context =>
                context.Context.Response.Headers[HeaderNames.CacheControl] = "public,max-age=86400",
        });

    /// <summary>
    /// Maps the OpenAPI document (<c>/openapi/v1.json</c>) and the Scalar UI (<c>/scalar</c>).
    /// </summary>
    public static WebApplication MapApiDocumentation(this WebApplication app)
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options => options
            .WithTitle("Lodge API")
            .AddDocument(DependencyInjection.OpenApiDocumentName));

        app.MapGet("/", () => Results.Redirect("/scalar")).ExcludeFromDescription();

        return app;
    }

    /// <summary>
    /// <c>/health</c> checks every dependency (detailed JSON); <c>/health/live</c> only checks that the process answers.
    /// </summary>
    public static WebApplication MapLodgeHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
        });

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
        });

        return app;
    }
}
