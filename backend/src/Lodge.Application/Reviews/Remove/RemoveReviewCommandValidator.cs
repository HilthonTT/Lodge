using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Reviews.Remove;

/// <summary>
/// Represents the <see cref="RemoveReviewCommand"/> validator.
/// </summary>
internal sealed class RemoveReviewCommandValidator : AbstractValidator<RemoveReviewCommand>
{
    /// <summary>
    /// Initializes a new instance of <see cref="RemoveReviewCommandValidator"/> class.
    /// </summary>
    public RemoveReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty().WithError(ValidationErrors.RemoveReview.ReviewIdIsRequired);
    }
}
