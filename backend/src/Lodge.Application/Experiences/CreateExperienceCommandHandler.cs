using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Abstractions.Storage;
using Lodge.Application.Hosting;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Experiences;

namespace Lodge.Application.Experiences;

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
