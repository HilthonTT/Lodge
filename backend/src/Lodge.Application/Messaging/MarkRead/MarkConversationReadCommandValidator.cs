using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Messaging.MarkRead;

internal sealed class MarkConversationReadCommandValidator : AbstractValidator<MarkConversationReadCommand>
{
    public MarkConversationReadCommandValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty()
            .WithError(ValidationErrors.Messaging.ConversationIdIsRequired);
    }
}
