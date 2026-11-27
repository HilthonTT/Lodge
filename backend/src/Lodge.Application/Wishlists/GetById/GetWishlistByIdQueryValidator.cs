using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Wishlists.GetById;

internal sealed class GetWishlistByIdQueryValidator : AbstractValidator<GetWishlistByIdQuery>
{
    public GetWishlistByIdQueryValidator()
    {
        RuleFor(x => x.WishlistId)
            .NotEmpty()
            .WithError(ValidationErrors.Wishlists.WishlistIdIsRequired);
    }
}
