using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;
using Lodge.Domain.Messaging;

namespace Lodge.Application.Messaging.GetMessages;

internal sealed class GetMessagesQueryValidator : AbstractValidator<GetMessagesQuery>
{
    public GetMessagesQueryValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty()
            .WithError(ValidationErrors.Messaging.ConversationIdIsRequired);

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, Message.MaxPageSize)
            .WithError(MessagingErrors.InvalidLimit);
    }
}
