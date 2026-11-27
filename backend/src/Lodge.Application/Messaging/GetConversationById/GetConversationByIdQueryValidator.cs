using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Messaging.GetConversationById;

internal sealed class GetConversationByIdQueryValidator : AbstractValidator<GetConversationByIdQuery>
{
    public GetConversationByIdQueryValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty()
            .WithError(ValidationErrors.Messaging.ConversationIdIsRequired);
    }
}
