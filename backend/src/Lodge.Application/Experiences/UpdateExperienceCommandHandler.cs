using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Abstractions.Storage;
using Lodge.Application.Hosting;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Experiences;

namespace Lodge.Application.Experiences;

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
