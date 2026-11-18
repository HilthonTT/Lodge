using Lodge.Application.Hosting;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Experiences;

namespace Lodge.Application.Experiences;

/// <summary>
/// Loads an experience the caller hosts (or any experience for admins).
/// </summary>
internal static class HostExperiences
{
    public static async Task<Result<(HostCaller Caller, Experience Experience)>> GetManagedAsync(
        IHostAccess hostAccess,
        IExperienceRepository experienceRepository,
        Guid experienceId,
        CancellationToken cancellationToken)
    {
        Result<HostCaller> caller = await hostAccess.GetCallerAsync(cancellationToken);

        if (caller.IsFailure)
        {
            return Result.Failure<(HostCaller, Experience)>(caller.Error);
        }

        Experience? experience = await experienceRepository.GetByIdAsync(experienceId, cancellationToken);

        if (experience is null)
        {
            return Result.Failure<(HostCaller, Experience)>(ExperienceErrors.NotFound(experienceId));
        }

        if (!caller.Value.IsAdmin && !experience.IsHostedBy(caller.Value.UserId))
        {
            return Result.Failure<(HostCaller, Experience)>(ExperienceErrors.NotHost);
        }

        return (caller.Value, experience);
    }
}
