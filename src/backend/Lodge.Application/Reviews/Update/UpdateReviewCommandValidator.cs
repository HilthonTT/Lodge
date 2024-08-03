using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;
using Lodge.Domain.Reviews;

namespace Lodge.Application.Reviews.Update;

/// <summary>
/// Represents the <see cref="UpdateReviewCommand"/> validator.
/// </summary>
internal sealed class UpdateReviewCommandValidator : AbstractValidator<UpdateReviewCommand>
{
    /// <summary>
    /// Initializes a new instance of <see cref="UpdateReviewCommandValidator"/> class.
    /// </summary>
    public UpdateReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty().WithError(ValidationErrors.UpdateReview.ReviewIdIsRequired);

        RuleFor(x => x.UserId).NotEmpty().WithError(ValidationErrors.UpdateReview.UserIdIsRequired);

        RuleFor(x => x.Rating)
            .LessThan(5).WithError(ValidationErrors.UpdateReview.RatingMustBeBelowFive)
            .GreaterThanOrEqualTo(0).WithError(ValidationErrors.UpdateReview.RatingMustBeGreaterOrEqualToZero);

        RuleFor(x => x.Comment)
            .NotEmpty().WithError(ValidationErrors.UpdateReview.CommentIsRequired)
            .MaximumLength(Comment.MaxLength).WithError(ValidationErrors.UpdateReview.CommentMustNotBeAbove(Comment.MaxLength));
    }
}
