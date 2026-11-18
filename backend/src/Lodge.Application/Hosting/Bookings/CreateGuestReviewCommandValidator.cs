using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;
using Lodge.Domain.Reviews;

namespace Lodge.Application.Hosting.Bookings;

internal sealed class CreateGuestReviewCommandValidator : AbstractValidator<CreateGuestReviewCommand>
{
    public CreateGuestReviewCommandValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithError(ValidationErrors.CreateReview.RatingMustBeBetweenOneAndFive);

        RuleFor(x => x.Comment)
            .NotEmpty().WithError(ValidationErrors.CreateReview.CommentIsRequired)
            .MaximumLength(Comment.MaxLength).WithError(ValidationErrors.CreateReview.CommentMustNotBeAbove(Comment.MaxLength));
    }
}
