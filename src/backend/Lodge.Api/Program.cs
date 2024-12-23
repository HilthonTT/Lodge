using Asp.Versioning;
using Asp.Versioning.Builder;
using Hangfire;
using HealthChecks.UI.Client;
using Lodge.Api.Extensions;
using Lodge.Application;
using Lodge.BackgroundTasks;
using Lodge.Infrastructure;
using Lodge.Persistence;
using Lodge.Presentation;
using Lodge.Presentation.Extensions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig.ReadFrom.Configuration(context.Configuration));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddInfrastructure(builder.Configuration)
    .AddPersistence(builder.Configuration)
    .AddBackgroundTasks(builder.Configuration)
    .AddApplication(builder.Configuration)
    .AddPresentation();

builder.Services.AddFrontendCors(builder.Configuration);

WebApplication app = builder.Build();

ApiVersionSet apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1))
    .ReportApiVersions()
    .Build();

RouteGroupBuilder versionedGroup = app
    .MapGroup("api/v{version:apiVersion}")
    .WithApiVersionSet(apiVersionSet);

app.MapEndpoints(versionedGroup);

app.UseCors("AllowSpecificOrigin");

app.UseBackgroundJobs();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseHangfireDashboard(options: new DashboardOptions
    {
        Authorization = [],
        DarkModeEnabled = false
    });

    app.ApplyMigrations();

    // app.SeedData();
}

app.UseHttpsRedirection();

app.MapHealthChecks("health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseAuthentication();

app.UseAuthorization();

app.UseRequestContextLogging();

app.UseSerilogRequestLogging();

app.UseStaticFiles();

app.UseExceptionHandler();

app.Run();

public partial class Program;
