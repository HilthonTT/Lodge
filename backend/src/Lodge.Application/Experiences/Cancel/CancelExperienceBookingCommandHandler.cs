using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Experiences;

namespace Lodge.Application.Experiences.Cancel;

internal sealed class CancelExperienceBookingCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IExperienceRepository experienceRepository,
    ExperiencePayments experiencePayments,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CancelExperienceBookingCommand>
{
    public async Task<Result> Handle(CancelExperienceBookingCommand request, CancellationToken cancellationToken)
    {
        ExperienceBooking? booking = await experienceRepository.GetBookingAsync(request.ExperienceBookingId, cancellationToken);

        if (booking is null || booking.GuestId != userIdentifierProvider.UserId)
        {
            return Result.Failure(ExperienceBookingErrors.NotFound(request.ExperienceBookingId));
        }

        ExperienceSession? session = await experienceRepository.GetSessionForUpdateAsync(booking.SessionId, cancellationToken);

        if (session is null)
        {
            return Result.Failure(ExperienceSessionErrors.NotFound(booking.SessionId));
        }

        // The booking was read before the lock: a concurrent cancel may already have released its seats.
        await experienceRepository.ReloadBookingAsync(booking, cancellationToken);

        Result<decimal> cancelled = booking.Cancel(session, dateTimeProvider.UtcNow, byHost: false);

        if (cancelled.IsFailure)
        {
            return cancelled;
        }

        await experiencePayments.AdjustPayoutAsync(booking, cancelled.Value, cancellationToken);

        return Result.Success();
    }
}
