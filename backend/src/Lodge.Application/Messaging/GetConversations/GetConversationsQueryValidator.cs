using FluentValidation;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Messaging.GetConversations;

internal sealed class GetConversationsQueryValidator : AbstractValidator<GetConversationsQuery>
{
    public GetConversationsQueryValidator()
    {
        RuleFor(x => x.Page).ValidPage();
        RuleFor(x => x.PageSize).ValidPageSize();
    }
}
