using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Core.Extensions;

/// <summary>
/// Contains extension methods for FluentValidation.
/// </summary>
internal static class FluentValidationExtensions
{
    /// <summary>
    /// Uses the specified <see cref="Error"/> code and description for the rule's failure.
    /// </summary>
    public static IRuleBuilderOptions<T, TProperty> WithError<T, TProperty>(
        this IRuleBuilderOptions<T, TProperty> rule,
        Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return rule.WithErrorCode(error.Code).WithMessage(error.Description);
    }

    public static IRuleBuilderOptions<T, int> ValidPage<T>(this IRuleBuilder<T, int> rule) =>
        rule.GreaterThanOrEqualTo(1).WithError(ValidationErrors.Paging.PageMustBePositive);

    public static IRuleBuilderOptions<T, int> ValidPageSize<T>(this IRuleBuilder<T, int> rule) =>
        rule.InclusiveBetween(1, ValidationErrors.Paging.MaxPageSize).WithError(ValidationErrors.Paging.PageSizeOutOfRange);
}
