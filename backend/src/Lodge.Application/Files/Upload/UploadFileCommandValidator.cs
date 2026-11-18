using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;
using Lodge.Application.Users.Avatar;

namespace Lodge.Application.Files.Upload;

internal sealed class UploadFileCommandValidator : AbstractValidator<UploadFileCommand>
{
    public const long MaxFileSizeInBytes = 5 * 1024 * 1024;

    public UploadFileCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithError(ValidationErrors.UploadFile.UserIdIsRequired);

        RuleFor(x => x.Length)
            .GreaterThan(0).WithError(ValidationErrors.UploadFile.FileIsRequired)
            .LessThanOrEqualTo(MaxFileSizeInBytes).WithError(ValidationErrors.UploadFile.FileTooLarge(MaxFileSizeInBytes));

        RuleFor(x => x.ContentType)
            .NotEmpty().WithError(ValidationErrors.UploadFile.ContentTypeIsRequired)
            .Must(contentType => ImageFormats.AllowedContentTypes.Contains(contentType))
            .WithError(ValidationErrors.UploadFile.InvalidContentType);
    }
}
