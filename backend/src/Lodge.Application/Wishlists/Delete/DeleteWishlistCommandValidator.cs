using FluentValidation;
using Lodge.Application.Core.Errors;
using Lodge.Application.Core.Extensions;

namespace Lodge.Application.Wishlists.Delete;

internal sealed class DeleteWishlistCommandValidator : AbstractValidator<DeleteWishlistCommand>
{
    public DeleteWishlistCommandValidator()
    {
        RuleFor(x => x.WishlistId)
            .NotEmpty()
            .WithError(ValidationErrors.Wishlists.WishlistIdIsRequired);
    }
}
