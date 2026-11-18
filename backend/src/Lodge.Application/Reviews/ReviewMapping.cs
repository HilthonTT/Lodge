using FluentValidation;
using Lodge.Application.Core.Extensions;
using Lodge.Contracts.Reviews;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Reviews;

namespace Lodge.Application.Reviews;

/// <summary>
/// Validation and mapping of category ratings.
/// </summary>
internal static class ReviewMapping
{
    public static Result<CategoryRatings> ToCategories(CategoryRatingsDto? categories) =>
        categories is null
            ? Result.Failure<CategoryRatings>(ReviewErrors.InvalidCategoryRating)
            : CategoryRatings.Create(
                categories.Cleanliness,
                categories.Accuracy,
                categories.CheckIn,
                categories.Communication,
                categories.Location,
                categories.Value);

    /// <summary>
    /// Requires every category rating (1 to 5).
    /// </summary>
    public static IRuleBuilderOptions<T, CategoryRatingsDto?> ValidCategories<T>(this IRuleBuilder<T, CategoryRatingsDto?> rule) =>
        rule.Must(categories => ToCategories(categories).IsSuccess).WithError(ReviewErrors.InvalidCategoryRating);
}
