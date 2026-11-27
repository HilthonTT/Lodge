using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Messaging.Send;

internal sealed class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty()
            .WithError(ValidationErrors.Messaging.ConversationIdIsRequired);

        RuleFor(x => x.Body).ValidMessageBody();
    }
}
