using FluentValidation;

namespace Lodge.Application.Wishlists;

internal sealed class RenameWishlistCommandValidator : AbstractValidator<RenameWishlistCommand>
{
    public RenameWishlistCommandValidator() => RuleFor(x => x.Name).ValidWishlistName();
}
