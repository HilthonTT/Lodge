namespace Lodge.Domain.Core.Primitives;

/// <summary>
/// Represents a concrete domain error.
/// </summary>
public record Error
{
    /// <summary>
    /// Gets the empty error instance.
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    /// <summary>
    /// Gets the general null error instance.
    /// </summary>
    public static readonly Error NullValue = new(
        "General.Null",
        "Null value was provided",
        ErrorType.Failure);

    /// <summary>
    /// Initializes a new instance of the <see cref="Error"/> record.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="description">The error description.</param>
    /// <param name="type">The error type.</param>
    public Error(string code, string description, ErrorType type)
    {
        Code = code;
        Description = description;
        Type = type;
    }

    /// <summary>
    /// Gets the error code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the error description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the error type.
    /// </summary>
    public ErrorType Type { get; }

    /// <summary>
    /// Creates an <see cref="Error"/> with <see cref="ErrorType.Failure"/>.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="description">The error description.</param>
    /// <returns>An instance of <see cref="Error"/> with type <see cref="ErrorType.Failure"/>.</returns>
    public static Error OnFailure(string code, string description) =>
        new(code, description, ErrorType.Failure);

    /// <summary>
    /// Creates an <see cref="Error"/> with <see cref="ErrorType.NotFound"/>.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="description">The error description.</param>
    /// <returns>An instance of <see cref="Error"/> with type <see cref="ErrorType.NotFound"/>.</returns>
    public static Error NotFound(string code, string description) =>
        new(code, description, ErrorType.NotFound);

    /// <summary>
    /// Creates an <see cref="Error"/> with <see cref="ErrorType.Problem"/>.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="description">The error description.</param>
    /// <returns>An instance of <see cref="Error"/> with type <see cref="ErrorType.Problem"/>.</returns>
    public static Error Problem(string code, string description) =>
        new(code, description, ErrorType.Problem);

    /// <summary>
    /// Creates an <see cref="Error"/> with <see cref="ErrorType.Conflict"/>.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="description">The error description.</param>
    /// <returns>An instance of <see cref="Error"/> with type <see cref="ErrorType.Conflict"/>.</returns>
    public static Error Conflict(string code, string description) =>
        new(code, description, ErrorType.Conflict);

    /// <summary>
    /// Creates an <see cref="Error"/> with <see cref="ErrorType.Permission"/>.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="description">The error description.</param>
    /// <returns>An instance of <see cref="Error"/> with type <see cref="ErrorType.Permission"/>.</returns>
    public static Error Permission(string code, string description) =>
        new(code, description, ErrorType.Permission);
}
