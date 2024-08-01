namespace Lodge.Application.Core.Exceptions;

/// <summary>
/// Represents the concurrency exception.
/// </summary>
/// <param name="message">The message.</param>
/// <param name="innerException">The inner exception.</param>
public sealed class ConcurrencyException(string? message, Exception? innerException)
    : Exception(message, innerException)
{
}
