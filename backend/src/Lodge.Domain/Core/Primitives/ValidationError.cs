namespace Lodge.Domain.Core.Primitives;

/// <summary>
/// Represents a validation error.
/// </summary>
public sealed record ValidationError : Error
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationError"/> record.
    /// </summary>
    /// <param name="errors">The errors.</param>
    public ValidationError(Error[] errors) 
        : base("Validation.General", "One or more validation errors occured", ErrorType.Validation)
    {
        Errors = errors;
    }

    /// <summary>
    /// Gets the errors.
    /// </summary>
    public Error[] Errors { get; }
}
