using FluentValidation;

namespace Lodge.Application.Wishlists;

internal sealed class CreateWishlistCommandValidator : AbstractValidator<CreateWishlistCommand>
{
    public CreateWishlistCommandValidator() => RuleFor(x => x.Name).ValidWishlistName();
}
