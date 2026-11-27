using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Messaging.StartConversation;

internal sealed class StartConversationCommandValidator : AbstractValidator<StartConversationCommand>
{
    public StartConversationCommandValidator()
    {
        RuleFor(x => x.ApartmentId)
            .NotEmpty()
            .WithError(ValidationErrors.Messaging.ApartmentIdIsRequired);

        RuleFor(x => x.Message).ValidMessageBody();
    }
}
