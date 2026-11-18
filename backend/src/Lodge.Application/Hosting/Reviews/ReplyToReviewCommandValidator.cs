using FluentValidation;
using Lodge.Application.Core.Extensions;
using Lodge.Domain.Reviews;

namespace Lodge.Application.Hosting.Reviews;

internal sealed class ReplyToReviewCommandValidator : AbstractValidator<ReplyToReviewCommand>
{
    public ReplyToReviewCommandValidator() =>
        RuleFor(x => x.Text)
            .Must(text => !string.IsNullOrWhiteSpace(text)).WithError(ReviewErrors.ReplyEmpty)
            .Must(text => text is null || text.Trim().Length <= Review.ReplyMaxLength).WithError(ReviewErrors.ReplyTooLong);
}
