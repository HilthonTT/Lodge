using Lodge.Application.Abstractions.Data;
using Lodge.Application.Payments;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Experiences;
using Lodge.Domain.Payouts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Experiences;

/// <summary>
/// The money side of experience bookings: the guest service fee, confirming a payment (which schedules the host payout
/// for the day after the session) and adjusting the payout of a cancelled booking. Changes are saved by the caller.
/// </summary>
internal sealed class ExperiencePayments(
    IDbContext context,
    IExperienceRepository experienceRepository,
    IPayoutRepository payoutRepository,
    IOptions<PaymentsOptions> paymentsOptions,
    IDateTimeProvider dateTimeProvider)
{
    public decimal GuestServiceFeePercent => paymentsOptions.Value.GuestServiceFeePercent;

    public async Task<Result> ConfirmPaymentAsync(
        ExperienceBooking booking,
        string paymentIntentId,
        decimal amountPaid,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(booking);

        // Serialise with the hold-expiry job and cancellations (which lock the session), then re-read the booking.
        await experienceRepository.GetSessionForUpdateAsync(booking.SessionId, cancellationToken);
        await experienceRepository.ReloadBookingAsync(booking, cancellationToken);

        if (booking.Status is ExperienceBookingStatus.Confirmed or ExperienceBookingStatus.Completed)
        {
            return Result.Success();
        }

        Result confirmed = booking.ConfirmPayment(paymentIntentId, amountPaid, dateTimeProvider.UtcNow);

        if (confirmed.IsFailure)
        {
            return confirmed;
        }

        Guid? hostId = await context.Set<Experience>()
            .IgnoreQueryFilters()
            .Where(experience => experience.Id == booking.ExperienceId)
            .Select(experience => (Guid?)experience.HostId)
            .FirstOrDefaultAsync(cancellationToken);

        if (hostId is null)
        {
            return Result.Failure(ExperienceErrors.NotFound(booking.ExperienceId));
        }

        if (await payoutRepository.GetByExperienceBookingIdAsync(booking.Id, cancellationToken) is null)
        {
            payoutRepository.Insert(Payout.ScheduleExperience(booking, hostId.Value, paymentsOptions.Value.HostServiceFeePercent));
        }

        return Result.Success();
    }

    /// <summary>
    /// A refunded booking cancels the payout; a late cancellation (no refund) keeps it.
    /// </summary>
    public async Task AdjustPayoutAsync(ExperienceBooking booking, decimal refundAmount, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(booking);

        Payout? payout = await payoutRepository.GetByExperienceBookingIdAsync(booking.Id, cancellationToken);

        payout?.AdjustForCancellation(refundAmount > 0 || booking.CancelledByHost ? 0 : booking.PriceTotal);
    }
}
