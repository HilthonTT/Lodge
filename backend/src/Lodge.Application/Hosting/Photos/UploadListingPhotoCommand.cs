using FluentValidation;
using Lodge.Application.Abstractions.App;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Abstractions.Storage;
using Lodge.Application.Core.Extensions;
using Lodge.Application.Users.Avatar;
using Lodge.Contracts.Hosting;
using Lodge.Domain.Apartments;
using Lodge.Domain.Core.Primitives;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Hosting.Photos;

/// <summary>
/// Uploads a listing photo (JPEG, PNG or WebP, detected from the bytes; at most 10 MB). The returned file id is then
/// passed in a listing's <c>imageIds</c>.
/// </summary>
public sealed record UploadListingPhotoCommand(Stream Stream, long Length) : ICommand<PhotoUploadResponse>
{
    public const long MaxFileSizeInBytes = 10 * 1024 * 1024;
}

internal sealed class UploadListingPhotoCommandValidator : AbstractValidator<UploadListingPhotoCommand>
{
    public UploadListingPhotoCommandValidator() =>
        RuleFor(x => x.Length)
            .GreaterThan(0).WithError(ApartmentErrors.InvalidPhoto)
            .LessThanOrEqualTo(UploadListingPhotoCommand.MaxFileSizeInBytes).WithError(ApartmentErrors.PhotoTooLarge);
}

internal sealed class UploadListingPhotoCommandHandler(
    IHostAccess hostAccess,
    IBlobService blobService,
    IOptions<AppOptions> appOptions) : ICommandHandler<UploadListingPhotoCommand, PhotoUploadResponse>
{
    public async Task<Result<PhotoUploadResponse>> Handle(
        UploadListingPhotoCommand request,
        CancellationToken cancellationToken)
    {
        Result<HostCaller> caller = await hostAccess.GetCallerAsync(cancellationToken);

        if (caller.IsFailure)
        {
            return Result.Failure<PhotoUploadResponse>(caller.Error);
        }

        // The declared content type is not trusted: the file is buffered and its leading bytes are checked.
        using var buffer = new MemoryStream();
        await request.Stream.CopyToAsync(buffer, cancellationToken);

        if (buffer.Length > UploadListingPhotoCommand.MaxFileSizeInBytes)
        {
            return Result.Failure<PhotoUploadResponse>(ApartmentErrors.PhotoTooLarge);
        }

        string? contentType = ImageFormats.Detect(buffer.GetBuffer().AsSpan(0, (int)Math.Min(buffer.Length, 16)));

        if (contentType is null)
        {
            return Result.Failure<PhotoUploadResponse>(ApartmentErrors.InvalidPhoto);
        }

        buffer.Position = 0;

        Guid fileId = await blobService.UploadAsync(caller.Value.UserId, buffer, contentType, cancellationToken);

        return new PhotoUploadResponse(fileId, appOptions.Value.FileUrl(fileId));
    }
}
