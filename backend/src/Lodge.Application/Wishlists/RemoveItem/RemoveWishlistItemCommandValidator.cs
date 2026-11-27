using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Wishlists.RemoveItem;

internal sealed class RemoveWishlistItemCommandValidator : AbstractValidator<RemoveWishlistItemCommand>
{
    public RemoveWishlistItemCommandValidator()
    {
        RuleFor(x => x.WishlistId)
            .NotEmpty()
            .WithError(ValidationErrors.Wishlists.WishlistIdIsRequired);

        RuleFor(x => x.ApartmentId)
            .NotEmpty()
            .WithError(ValidationErrors.Wishlists.ApartmentIdIsRequired);
    }
}
