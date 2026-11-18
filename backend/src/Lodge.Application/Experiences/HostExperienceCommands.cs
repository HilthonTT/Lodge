using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Abstractions.Storage;
using Lodge.Application.Hosting;
using Lodge.Contracts.Experiences;
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

/// <summary>
/// Creates an experience hosted by the caller (who needs a verified e-mail address).
/// </summary>
public sealed record CreateExperienceCommand(ExperienceRequest Experience) : ICommand<Guid>;

internal sealed class CreateExperienceCommandHandler(
    IHostAccess hostAccess,
    IExperienceRepository experienceRepository,
    IBlobService blobService) : ICommandHandler<CreateExperienceCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateExperienceCommand request, CancellationToken cancellationToken)
    {
        Result<HostCaller> caller = await hostAccess.GetCallerAsync(cancellationToken);

        if (caller.IsFailure)
        {
            return Result.Failure<Guid>(caller.Error);
        }

        Result<ExperienceDetails> details = ExperienceMapper.ToDetails(request.Experience);

        if (details.IsFailure)
        {
            return Result.Failure<Guid>(details.Error);
        }

        Result<List<Guid>> images = await ExperienceMapper.ResolveImagesAsync(
            blobService,
            null,
            request.Experience.ImageIds,
            caller.Value.UserId,
            cancellationToken);

        if (images.IsFailure)
        {
            return Result.Failure<Guid>(images.Error);
        }

        Result<Experience> experience = Experience.Create(
            caller.Value.UserId,
            details.Value,
            images.Value,
            request.Experience.IsListed ?? true);

        if (experience.IsFailure)
        {
            return Result.Failure<Guid>(experience.Error);
        }

        experienceRepository.Insert(experience.Value);

        return experience.Value.Id;
    }
}

/// <summary>
/// Replaces the details and photos of an experience.
/// </summary>
public sealed record UpdateExperienceCommand(Guid ExperienceId, ExperienceRequest Experience) : ICommand;

internal sealed class UpdateExperienceCommandHandler(
    IHostAccess hostAccess,
    IExperienceRepository experienceRepository,
    IBlobService blobService) : ICommandHandler<UpdateExperienceCommand>
{
    public async Task<Result> Handle(UpdateExperienceCommand request, CancellationToken cancellationToken)
    {
        var managed = await HostExperiences.GetManagedAsync(hostAccess, experienceRepository, request.ExperienceId, cancellationToken);

        if (managed.IsFailure)
        {
            return managed;
        }

        (HostCaller caller, Experience experience) = managed.Value;

        Result<ExperienceDetails> details = ExperienceMapper.ToDetails(request.Experience);

        if (details.IsFailure)
        {
            return details;
        }

        Result<List<Guid>> images = await ExperienceMapper.ResolveImagesAsync(
            blobService,
            experience,
            request.Experience.ImageIds,
            caller.UserId,
            cancellationToken);

        if (images.IsFailure)
        {
            return images;
        }

        return experience.Update(details.Value, images.Value);
    }
}

/// <summary>
/// Lists or unlists an experience.
/// </summary>
public sealed record SetExperienceStatusCommand(Guid ExperienceId, bool IsListed) : ICommand;

internal sealed class SetExperienceStatusCommandHandler(
    IHostAccess hostAccess,
    IExperienceRepository experienceRepository) : ICommandHandler<SetExperienceStatusCommand>
{
    public async Task<Result> Handle(SetExperienceStatusCommand request, CancellationToken cancellationToken)
    {
        var managed = await HostExperiences.GetManagedAsync(hostAccess, experienceRepository, request.ExperienceId, cancellationToken);

        if (managed.IsFailure)
        {
            return managed;
        }

        managed.Value.Experience.SetListed(request.IsListed);

        return Result.Success();
    }
}

/// <summary>
/// Deletes (soft) an experience without upcoming reserved or confirmed bookings (409 otherwise).
/// </summary>
public sealed record DeleteExperienceCommand(Guid ExperienceId) : ICommand;

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
