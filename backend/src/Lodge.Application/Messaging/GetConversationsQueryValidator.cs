using FluentValidation;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Messaging;

internal sealed class GetConversationsQueryValidator : AbstractValidator<GetConversationsQuery>
{
    public GetConversationsQueryValidator()
    {
        RuleFor(x => x.Page).ValidPage();
        RuleFor(x => x.PageSize).ValidPageSize();
    }
}
