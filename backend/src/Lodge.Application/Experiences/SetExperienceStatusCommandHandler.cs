using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Hosting;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Experiences;

namespace Lodge.Application.Experiences;

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
