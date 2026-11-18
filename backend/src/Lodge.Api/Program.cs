using Asp.Versioning;
using Asp.Versioning.Builder;
using Lodge.Api.Extensions;
using Lodge.Application;
using Lodge.BackgroundTasks;
using Lodge.Infrastructure;
using Lodge.Persistence;
using Lodge.Presentation;
using Lodge.Presentation.Extensions;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services));

builder.Services
    .AddApplication()
    .AddInfrastructure()
    .AddPersistence()
    .AddBackgroundTasks()
    .AddPresentation()
    .AddDatabaseStartup()
    .AddFrontendCors(builder.Configuration);

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// Static assets (apartment images) are served before request logging to keep the logs readable.
app.UseApartmentImages();

app.UseRequestContextLogging();
app.UseSerilogRequestLogging();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

await app.InitializeDatabaseAsync();

app.UseBackgroundJobs();

if (app.Environment.IsDevelopment())
{
    app.MapApiDocumentation();
    app.MapHangfireDashboard();
}

ApiVersionSet apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1))
    .ReportApiVersions()
    .Build();

RouteGroupBuilder versionedGroup = app
    .MapGroup("api/v{version:apiVersion}")
    .WithApiVersionSet(apiVersionSet);

app.MapEndpoints(versionedGroup);

app.MapLodgeHealthChecks();

await app.RunAsync();

/// <summary>
/// Entry point; public so that WebApplicationFactory-based tests can reference it.
/// </summary>
public partial class Program;
