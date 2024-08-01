using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Core.Errors;

/// <summary>
/// Contains the blob errors.
/// </summary>
public static class BlobErrors
{
    public static Error NotFound(Guid fileId) => Error.NotFound(
        "Blob.NotFound", $"The blob with Id = '{fileId}' was not found");
}
