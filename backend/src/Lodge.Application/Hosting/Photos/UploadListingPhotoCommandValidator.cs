using FluentValidation;
using Lodge.Application.Core.Extensions;
using Lodge.Domain.Apartments;

namespace Lodge.Application.Hosting.Photos;

internal sealed class UploadListingPhotoCommandValidator : AbstractValidator<UploadListingPhotoCommand>
{
    public UploadListingPhotoCommandValidator() =>
        RuleFor(x => x.Length)
            .GreaterThan(0).WithError(ApartmentErrors.InvalidPhoto)
            .LessThanOrEqualTo(UploadListingPhotoCommand.MaxFileSizeInBytes).WithError(ApartmentErrors.PhotoTooLarge);
}
