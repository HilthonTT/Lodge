using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Hosting;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Experiences;

namespace Lodge.Application.Experiences.DeleteSession;

internal sealed class DeleteExperienceSessionCommandHandler(
    IHostAccess hostAccess,
    IExperienceRepository experienceRepository,
    ExperiencePayments experiencePayments,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<DeleteExperienceSessionCommand>
{
    public async Task<Result> Handle(DeleteExperienceSessionCommand request, CancellationToken cancellationToken)
    {
        var managed = await HostExperiences.GetManagedAsync(hostAccess, experienceRepository, request.ExperienceId, cancellationToken);

        if (managed.IsFailure)
        {
            return managed;
        }

        ExperienceSession? session = await experienceRepository.GetSessionForUpdateAsync(request.SessionId, cancellationToken);

        if (session is null || session.ExperienceId != request.ExperienceId)
        {
            return Result.Failure(ExperienceSessionErrors.NotFound(request.SessionId));
        }

        List<ExperienceBooking> bookings = await experienceRepository.GetActiveBookingsOfSessionAsync(session.Id, cancellationToken);
        DateTime utcNow = dateTimeProvider.UtcNow;

        if (bookings.Count == 0 && !await experienceRepository.SessionHasBookingsAsync(session.Id, cancellationToken))
        {
            experienceRepository.RemoveSession(session);

            return Result.Success();
        }

        session.Cancel(utcNow);

        foreach (ExperienceBooking booking in bookings)
        {
            Result<decimal> cancelled = booking.Cancel(session, utcNow, byHost: true);

            if (cancelled.IsSuccess)
            {
                await experiencePayments.AdjustPayoutAsync(booking, cancelled.Value, cancellationToken);
            }
        }

        return Result.Success();
    }
}
