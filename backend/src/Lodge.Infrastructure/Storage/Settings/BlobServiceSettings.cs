namespace Lodge.Infrastructure.Storage.Settings;

internal sealed class BlobServiceSettings
{
    public const string SettingsKey = "BlobStorage";

    public string ContainerName { get; set; } = string.Empty;
}
