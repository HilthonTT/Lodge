namespace Lodge.Infrastructure.Messaging.Settings;

/// <summary>
/// Represents the message broker settings.
/// </summary>
internal sealed class MessageBrokerSettings
{
    public const string SettingsKey = "MessageBroker";

    /// <summary>
    /// Gets or sets the host name.
    /// </summary>
    public string HostName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the port.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets the user name.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    public string Password { get; private set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    public string QueueName { get; set; } = string.Empty;
}
