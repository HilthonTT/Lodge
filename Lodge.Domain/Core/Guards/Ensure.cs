using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Lodge.Domain.Core.Guards;

/// <summary>
/// Contains assertions for the most common application checks.
/// </summary>
public static class Ensure
{
    /// <summary>
    /// Ensures that the specified <see cref="string"/> value is not empty.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="paramName">The name of the parameter being checked.</param>
    /// <exception cref="ArgumentNullException">if the specified value is empty.</exception>
    public static void NotNullOrEmpty(
        [NotNull] string? value,
        [CallerArgumentExpression("value")] string? paramName = default)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentNullException(paramName);
        }
    }

    /// <summary>
    /// Ensures that the specified value is not null.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="paramName">The name of the parameter being checked.</param>
    /// <exception cref="ArgumentException">if the specified value is null</exception>
    public static void NotNull(
        [NotNull] object? value,
        [CallerArgumentExpression("value")] string? paramName = default)
    {
        if (value is null)
        {
            throw new ArgumentException(paramName);
        }
    }

    /// <summary>
    /// Ensures that the specified value <see cref="decimal"/> is greater than zero.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="paramName">The name of the parameter being checked.</param>
    /// <exception cref="ArgumentOutOfRangeException">if the specified value is 0 or smaller.</exception>
    public static void GreaterThanZero(
        decimal value,
        [CallerArgumentExpression("value")] string? paramName = default)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName);
        }
    }

    /// <summary>
    /// Ensures that the start date <see cref="DateTime"/> doesn't precede the end date <see cref="DateTime"/>.
    /// </summary>
    /// <param name="start">The start date.</param>
    /// <param name="end">The end date.</param>
    /// <param name="paramName">The name of the parameter being checked.</param>
    /// <exception cref="ArgumentOutOfRangeException">if the start date precedes the end date</exception>
    public static void StartDatePrecedesEndDate(
        DateTime start,
        DateTime end,
        [CallerArgumentExpression("end")] string? paramName = default)
    {
        if (start >= end)
        {
            throw new ArgumentOutOfRangeException(paramName);
        }
    }
}
