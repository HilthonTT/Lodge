namespace Lodge.Persistence.Infrastructure;

/// <summary>
/// Represents a connection string
/// </summary>
public sealed record ConnectionString(string Value)
{
    /// <summary>
    /// Represents the connection string key
    /// </summary>
    public const string SettingsKey = "Database";

    public static implicit operator string(ConnectionString connectionString) => connectionString.Value;
}
