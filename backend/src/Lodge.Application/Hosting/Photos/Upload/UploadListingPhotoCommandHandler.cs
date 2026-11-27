using Lodge.Application.Abstractions.App;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Abstractions.Storage;
using Lodge.Application.Users.Avatar;
using Lodge.Contracts.Hosting;
using Lodge.Domain.Apartments;
using Lodge.Domain.Core.Primitives;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Hosting.Photos.Upload;

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
        Result<BufferedImage> image = await ImageFormats.BufferAsync(
            request.Stream,
            UploadListingPhotoCommand.MaxFileSizeInBytes,
            ApartmentErrors.InvalidPhoto,
            ApartmentErrors.PhotoTooLarge,
            cancellationToken);

        if (image.IsFailure)
        {
            return Result.Failure<PhotoUploadResponse>(image.Error);
        }

        Guid fileId;

        using (BufferedImage buffered = image.Value)
        {
            fileId = await blobService.UploadAsync(
                caller.Value.UserId,
                buffered.Content,
                buffered.ContentType,
                cancellationToken);
        }

        return new PhotoUploadResponse(fileId, appOptions.Value.FileUrl(fileId));
    }
}
