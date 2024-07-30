using Hangfire;
using Lodge.Persistence.Outbox;

namespace Lodge.Api.Extensions;

/// <summary>
/// Contains extensions for the background jobs.
/// </summary>
public static class BackgroundJobExtensions
{
    /// <summary>
    /// Uses the background jobs.
    /// </summary>
    /// <param name="app">The web application app.</param>
    /// <returns>The same web application app.</returns>
    public static IApplicationBuilder UseBackgroundJobs(this WebApplication app)
    {
        IRecurringJobManager recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();

        recurringJobManager.AddOrUpdate<IProcessOutboxMessagesJob>(
            "outbox-processor",
            job => job.ProcessAsync(),
            app.Configuration["BackgroundJobs:Outbox:Schedule"]);

        return app;
    }
}
