using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Abstractions.Storage;
using Lodge.Application.Core.Errors;
using Lodge.Contracts.Storage;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Files.GetById;

/// <summary>
/// Represents the <see cref="GetFileByIdQuery"/> handler.
/// </summary>
/// <param name="blobService">The blob service.</param>
internal sealed class GetFileByIdQueryHandler(IBlobService blobService) : IQueryHandler<GetFileByIdQuery, FileResponse>
{
    public async Task<Result<FileResponse>> Handle(GetFileByIdQuery request, CancellationToken cancellationToken)
    {
        if (!await blobService.ExistsAsync(request.FileId, cancellationToken))
        {
            return Result.Failure<FileResponse>(BlobErrors.NotFound(request.FileId));
        }

        FileResponse fileResponse = await blobService.DownloadAsync(request.FileId, cancellationToken);

        return fileResponse;
    }
}
