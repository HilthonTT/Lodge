using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Hosting.Reviews.DeleteReply;

internal sealed class DeleteReviewReplyCommandValidator : AbstractValidator<DeleteReviewReplyCommand>
{
    public DeleteReviewReplyCommandValidator()
    {
        RuleFor(x => x.ReviewId)
            .NotEmpty()
            .WithError(ValidationErrors.Hosting.ReviewIdIsRequired);
    }
}
