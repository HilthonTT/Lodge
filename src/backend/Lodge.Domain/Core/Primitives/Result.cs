﻿using System.Diagnostics.CodeAnalysis;

namespace Lodge.Domain.Core.Primitives;

/// <summary>
/// Represents a result of some operation, with status information and possibly an error.
/// </summary>
public class Result
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> class with the specified parameters.
    /// </summary>
    /// <param name="isSuccess">
    /// The flag indicating if the result is successful.
    /// </param>
    /// <param name="error">
    /// The error.
    /// </param>
    /// <exception cref="ArgumentException">Exception is thrown when trying to create a "successful" error.</exception>
    public Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None ||
            !isSuccess && error == Error.None)
        {
            throw new ArgumentException("Invalid error", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Gets a value indicating whether the result is a success result.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets a value indicating whether the result is a failure result.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error.
    /// </summary>
    public Error Error { get; init; }

    /// <summary>
    /// Returns a success <see cref="Result"/>.
    /// </summary>
    /// <returns>A new instance of <see cref="Result"/> with the success flag set.</returns>
    public static Result Success() => new(true, Error.None);

    /// <summary>
    /// Returns a success <see cref="Result{TValue}"/> with the specified value.
    /// </summary>
    /// <typeparam name="TValue">The result type.</typeparam>
    /// <param name="value">The result value.</param>
    /// <returns>A new instance of <see cref="Result{TValue}"/> with the success flag set.</returns>
    public static Result<TValue> Success<TValue>(TValue value) =>
        new(value, true, Error.None);

    /// <summary>
    /// Returns a failure <see cref="Result"/> with the specified error.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>A new instance of <see cref="Result"/> with the specified error and failure flag set.</returns>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>
    /// Returns a failure <see cref="Result{TValue}"/> with the specified error.
    /// </summary>
    /// <typeparam name="TValue">The result type.</typeparam>
    /// <param name="error">The error.</param>
    /// <returns>A new instance of <see cref="Result{TValue}"/> with the specified error and failure flag set.</returns>
    /// <remarks>
    /// We're purposefully ignoring the nullable assignment here because the API will never allow it to be accessed.
    /// The value is accessed through a method that will throw an exception if the result is a failure result.
    /// </remarks>
    public static Result<TValue> Failure<TValue>(Error error) =>
        new(default, false, error);

    /// <summary>
    /// Returns the first failure from the specified <paramref name="results"/>.
    /// If there is no failure, a success is returned.
    /// </summary>
    /// <param name="results">The results array.</param>
    /// <returns>
    /// The first failure from the specified <paramref name="results"/> array, or a success it does not exist.
    /// </returns>
    public static Result FirstFailureOrSuccess(params Result[] results)
    {
        foreach (Result result in results)
        {
            if (result.IsFailure)
            {
                return result;
            }
        }

        return Success();
    }
}

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