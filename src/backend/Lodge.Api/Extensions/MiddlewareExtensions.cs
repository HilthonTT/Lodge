using Lodge.Api.Middlewares;

namespace Lodge.Api.Extensions;

/// <summary>
/// Contains the extensions methods for the middleware.
/// </summary>
public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseRequestContextLogging(this IApplicationBuilder app)
    {
        app.UseMiddleware<RequestContextLoggingMiddleware>();

        return app;
    }
}
