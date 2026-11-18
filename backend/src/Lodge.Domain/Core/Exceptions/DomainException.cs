using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Core.Exceptions;

/// <summary>
/// Represents an exception that occured in the domain.
/// </summary>
public sealed class DomainException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="error">The error containing the information about what happened.</param>
    public DomainException(Error error)
        : base(error.Description)
    {
        Error = error;
    }

    /// <summary>
    /// Gets the error.
    /// </summary>
    public Error Error { get; }
}
