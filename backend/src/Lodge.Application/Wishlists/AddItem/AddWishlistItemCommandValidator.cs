using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Wishlists.AddItem;

internal sealed class AddWishlistItemCommandValidator : AbstractValidator<AddWishlistItemCommand>
{
    public AddWishlistItemCommandValidator()
    {
        RuleFor(x => x.WishlistId)
            .NotEmpty()
            .WithError(ValidationErrors.Wishlists.WishlistIdIsRequired);

        RuleFor(x => x.ApartmentId)
            .NotEmpty()
            .WithError(ValidationErrors.Wishlists.ApartmentIdIsRequired);
    }
}
