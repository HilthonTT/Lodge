using System.ComponentModel.DataAnnotations;

namespace Lodge.Infrastructure.Storage;

/// <summary>
/// Blob storage settings (section <c>BlobStorage</c>). The connection string is <c>ConnectionStrings:BlobStorage</c>.
/// </summary>
public sealed class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    [Required]
    public string ContainerName { get; set; } = "files";
}
