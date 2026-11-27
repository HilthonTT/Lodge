using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Hosting;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Experiences;

namespace Lodge.Application.Experiences.Delete;

internal sealed class DeleteExperienceCommandHandler(
    IHostAccess hostAccess,
    IExperienceRepository experienceRepository,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<DeleteExperienceCommand>
{
    public async Task<Result> Handle(DeleteExperienceCommand request, CancellationToken cancellationToken)
    {
        var managed = await HostExperiences.GetManagedAsync(hostAccess, experienceRepository, request.ExperienceId, cancellationToken);

        if (managed.IsFailure)
        {
            return managed;
        }

        Experience experience = managed.Value.Experience;

        if (await experienceRepository.HasUpcomingBookingsAsync(experience.Id, dateTimeProvider.UtcNow, cancellationToken))
        {
            return Result.Failure(ExperienceErrors.HasUpcomingBookings);
        }

        experience.SetListed(false);
        experience.Deleted = true;
        experience.DeletedOnUtc = dateTimeProvider.UtcNow;

        return Result.Success();
    }
}
