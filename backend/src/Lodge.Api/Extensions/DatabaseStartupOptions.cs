namespace Lodge.Api.Extensions;

/// <summary>
/// Database actions performed at startup (section <c>Database</c>). Both are disabled unless configured,
/// and are enabled in <c>appsettings.Development.json</c>.
/// </summary>
internal sealed class DatabaseStartupOptions
{
    public const string SectionName = "Database";

    public bool ApplyMigrationsOnStartup { get; set; }

    public bool SeedDemoData { get; set; }
}
