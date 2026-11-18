using System.Diagnostics.CodeAnalysis;

namespace Lodge.Domain.Core.Primitives;

/// <summary>
/// Represents the result of some operation, with status information and possibly a value and an error.
/// </summary>
/// <typeparam name="TValue">The result value type.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="Result{TValueType}"/> class with the specified parameters.
/// </remarks>
/// <param name="value">The result value.</param>
/// <param name="isSuccess">The flag indicating if the result is successful.</param>
/// <param name="error">The error.</param>
public class Result<TValue>(TValue? value, bool isSuccess, Error error) : Result(isSuccess, error)
{
    private readonly TValue? _value = value;

    /// <summary>
    /// Gets the result value if the result is successful, otherwise throws an exception.
    /// </summary>
    /// <returns>The result value if the result is successful.</returns>
    /// <exception cref="InvalidOperationException"> when <see cref="Result.IsFailure"/> is true.</exception>
    [NotNull]
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failure result can't be accessed.");

    public static implicit operator Result<TValue>(TValue? value) =>
        value is not null ? Success(value) : Failure<TValue>(Error.NullValue);

    /// <summary>
    /// Returns a validation failure <see cref="Result{TValue}"/> with the specified error.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>A new instance of <see cref="Result"/> with the specified error and failure flag set.</returns>
    public static Result<TValue> ValidationFailure(Error error) =>
        new(default, false, error);
}
