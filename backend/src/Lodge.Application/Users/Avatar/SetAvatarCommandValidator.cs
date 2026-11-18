using FluentValidation;
using Lodge.Application.Core.Extensions;
using Lodge.Domain.Users;

namespace Lodge.Application.Users.Avatar;

internal sealed class SetAvatarCommandValidator : AbstractValidator<SetAvatarCommand>
{
    public const long MaxFileSizeInBytes = 5 * 1024 * 1024;

    public SetAvatarCommandValidator()
    {
        RuleFor(x => x.Length)
            .GreaterThan(0).WithError(UserErrors.InvalidAvatar)
            .LessThanOrEqualTo(MaxFileSizeInBytes).WithError(UserErrors.AvatarTooLarge);

        RuleFor(x => x.ContentType)
            .Must(contentType => contentType is not null && ImageFormats.AllowedContentTypes.Contains(contentType))
            .WithError(UserErrors.InvalidAvatar);
    }
}
