using Hangfire;
using Lodge.Application.Bookings.Complete;
using Lodge.Application.Bookings.Expire;
using Lodge.Application.Hosting.Superhost;
using Lodge.Persistence.Outbox;
using Microsoft.Extensions.Options;

namespace Lodge.Api.Extensions;

/// <summary>
/// Registers the recurring Hangfire jobs and the (development-only) dashboard.
/// </summary>
internal static class BackgroundJobExtensions
{
    public static WebApplication UseBackgroundJobs(this WebApplication app)
    {
        OutboxOptions outboxOptions = app.Services.GetRequiredService<IOptions<OutboxOptions>>().Value;
        CompleteBookingsOptions completeBookingsOptions =
            app.Services.GetRequiredService<IOptions<CompleteBookingsOptions>>().Value;

        IRecurringJobManager recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();

        recurringJobManager.AddOrUpdate<IProcessOutboxMessagesJob>(
            "outbox-processor",
            job => job.ProcessAsync(),
            outboxOptions.Schedule);

        recurringJobManager.AddOrUpdate<ICompleteBookingsJob>(
            "complete-finished-bookings",
            job => job.CompleteFinishedBookingsAsync(CancellationToken.None),
            completeBookingsOptions.Schedule);

        recurringJobManager.AddOrUpdate<IExpireBookingRequestsJob>(
            "expire-booking-requests",
            job => job.ExpireAsync(CancellationToken.None),
            app.Services.GetRequiredService<IOptions<ExpireBookingRequestsOptions>>().Value.Schedule);

        recurringJobManager.AddOrUpdate<IUpdateSuperhostStatusJob>(
            "update-superhost-status",
            job => job.UpdateAsync(CancellationToken.None),
            app.Services.GetRequiredService<IOptions<SuperhostJobOptions>>().Value.Schedule);

        return app;
    }

    /// <summary>
    /// Maps the Hangfire dashboard at <c>/hangfire</c> without authorization. Development only.
    /// </summary>
    public static WebApplication MapHangfireDashboard(this WebApplication app)
    {
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = [],
            DashboardTitle = "Lodge jobs",
        });

        return app;
    }
}
