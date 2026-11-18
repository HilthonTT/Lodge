using System.ComponentModel.DataAnnotations;

namespace Lodge.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ settings (section <c>MessageBroker</c>).
/// </summary>
public sealed class MessageBrokerOptions
{
    public const string SectionName = "MessageBroker";

    [Required]
    public string HostName { get; set; } = "localhost";

    [Range(1, 65535)]
    public int Port { get; set; } = 5672;

    [Required]
    public string UserName { get; set; } = "guest";

    [Required]
    public string Password { get; set; } = "guest";

    public string VirtualHost { get; set; } = "/";

    [Required]
    public string QueueName { get; set; } = "lodge-queue";
}
