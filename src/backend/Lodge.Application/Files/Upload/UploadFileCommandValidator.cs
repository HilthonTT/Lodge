using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Files.Upload;

/// <summary>
/// Represents the <see cref="UploadFileCommand"/> validator.
/// </summary>
internal sealed class UploadFileCommandValidator : AbstractValidator<UploadFileCommand>
{
    /// <summary>
    /// Initializes a new instance of <see cref="UploadFileCommandValidator"/> class.
    /// </summary>
    public UploadFileCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithError(ValidationErrors.UploadFile.UserIdIsRequired);

        RuleFor(x => x.ContentType).NotEmpty().WithError(ValidationErrors.UploadFile.ContentTypeIsRequired);
    }
}
