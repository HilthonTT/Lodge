using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;
using Lodge.Domain.Reviews;

namespace Lodge.Application.Reviews.Create;

/// <summary>
/// Represents the <see cref="CreateReviewCommand"/> validator.
/// </summary>
internal sealed class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    /// <summary>
    /// Initializes a new instance of <see cref="CreateReviewCommandValidator"/> class.
    /// </summary>
    public CreateReviewCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithError(ValidationErrors.CreateReview.UserIdIsRequired);

        RuleFor(x => x.BookingId).NotEmpty().WithError(ValidationErrors.CreateReview.BookingIdIsRequired);

        RuleFor(x => x.Rating)
            .LessThan(5).WithError(ValidationErrors.CreateReview.RatingMustBeBelowFive)
            .GreaterThanOrEqualTo(0).WithError(ValidationErrors.CreateReview.RatingMustBeGreaterOrEqualToZero);

        RuleFor(x => x.Comment)
            .NotEmpty().WithError(ValidationErrors.CreateReview.CommentIsRequired)
            .MaximumLength(Comment.MaxLength).WithError(ValidationErrors.CreateReview.CommentMustNotBeAbove(Comment.MaxLength));
    }
}
