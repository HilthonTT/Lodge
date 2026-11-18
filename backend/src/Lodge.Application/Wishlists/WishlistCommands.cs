using FluentValidation;
using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Core.Extensions;
using Lodge.Domain.Apartments;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Wishlists;

namespace Lodge.Application.Wishlists;

/// <summary>
/// Creates a wishlist for the current user (at most <see cref="Wishlist.MaxPerUser"/>).
/// </summary>
public sealed record CreateWishlistCommand(string? Name) : ICommand<Guid>;

/// <summary>
/// Renames one of the current user's wishlists.
/// </summary>
public sealed record RenameWishlistCommand(Guid WishlistId, string? Name) : ICommand;

/// <summary>
/// Deletes one of the current user's wishlists.
/// </summary>
public sealed record DeleteWishlistCommand(Guid WishlistId) : ICommand;

/// <summary>
/// Saves a listed apartment in one of the current user's wishlists (idempotent).
/// </summary>
public sealed record AddWishlistItemCommand(Guid WishlistId, Guid ApartmentId) : ICommand;

/// <summary>
/// Removes an apartment from one of the current user's wishlists (idempotent).
/// </summary>
public sealed record RemoveWishlistItemCommand(Guid WishlistId, Guid ApartmentId) : ICommand;

internal sealed class CreateWishlistCommandValidator : AbstractValidator<CreateWishlistCommand>
{
    public CreateWishlistCommandValidator() => RuleFor(x => x.Name).ValidWishlistName();
}

internal sealed class RenameWishlistCommandValidator : AbstractValidator<RenameWishlistCommand>
{
    public RenameWishlistCommandValidator() => RuleFor(x => x.Name).ValidWishlistName();
}

internal static class WishlistValidation
{
    public static IRuleBuilderOptions<T, string?> ValidWishlistName<T>(this IRuleBuilder<T, string?> rule) =>
        rule
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithError(WishlistErrors.NameRequired)
            .Must(name => name is null || name.Trim().Length <= Wishlist.NameMaxLength).WithError(WishlistErrors.NameTooLong);

    /// <summary>
    /// Loads a wishlist of the current user; other users' wishlists are "not found".
    /// </summary>
    public static async Task<Result<Wishlist>> GetOwnAsync(
        this IWishlistRepository repository,
        Guid wishlistId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        Wishlist? wishlist = await repository.GetByIdAsync(wishlistId, cancellationToken);

        return wishlist is not null && wishlist.UserId == userId
            ? wishlist
            : Result.Failure<Wishlist>(WishlistErrors.NotFound(wishlistId));
    }
}

internal sealed class CreateWishlistCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IWishlistRepository wishlistRepository,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CreateWishlistCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateWishlistCommand request, CancellationToken cancellationToken)
    {
        Guid userId = userIdentifierProvider.UserId;

        if (await wishlistRepository.CountByUserAsync(userId, cancellationToken) >= Wishlist.MaxPerUser)
        {
            return Result.Failure<Guid>(WishlistErrors.LimitReached);
        }

        Result<Wishlist> wishlist = Wishlist.Create(userId, request.Name, dateTimeProvider.UtcNow);

        if (wishlist.IsFailure)
        {
            return Result.Failure<Guid>(wishlist.Error);
        }

        wishlistRepository.Insert(wishlist.Value);

        return wishlist.Value.Id;
    }
}

internal sealed class RenameWishlistCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IWishlistRepository wishlistRepository,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<RenameWishlistCommand>
{
    public async Task<Result> Handle(RenameWishlistCommand request, CancellationToken cancellationToken)
    {
        Result<Wishlist> wishlist = await wishlistRepository.GetOwnAsync(
            request.WishlistId,
            userIdentifierProvider.UserId,
            cancellationToken);

        return wishlist.IsFailure ? wishlist : wishlist.Value.Rename(request.Name, dateTimeProvider.UtcNow);
    }
}

internal sealed class DeleteWishlistCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IWishlistRepository wishlistRepository) : ICommandHandler<DeleteWishlistCommand>
{
    public async Task<Result> Handle(DeleteWishlistCommand request, CancellationToken cancellationToken)
    {
        Result<Wishlist> wishlist = await wishlistRepository.GetOwnAsync(
            request.WishlistId,
            userIdentifierProvider.UserId,
            cancellationToken);

        if (wishlist.IsFailure)
        {
            return wishlist;
        }

        wishlistRepository.Remove(wishlist.Value);

        return Result.Success();
    }
}

internal sealed class AddWishlistItemCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IWishlistRepository wishlistRepository,
    IApartmentRepository apartmentRepository,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<AddWishlistItemCommand>
{
    public async Task<Result> Handle(AddWishlistItemCommand request, CancellationToken cancellationToken)
    {
        Result<Wishlist> wishlist = await wishlistRepository.GetOwnAsync(
            request.WishlistId,
            userIdentifierProvider.UserId,
            cancellationToken);

        if (wishlist.IsFailure)
        {
            return wishlist;
        }

        Apartment? apartment = await apartmentRepository.GetByIdAsync(request.ApartmentId, cancellationToken);

        if (apartment is not { IsListed: true })
        {
            return Result.Failure(ApartmentErrors.NotFound(request.ApartmentId));
        }

        wishlist.Value.AddItem(apartment.Id, dateTimeProvider.UtcNow);

        return Result.Success();
    }
}

internal sealed class RemoveWishlistItemCommandHandler(
    IUserIdentifierProvider userIdentifierProvider,
    IWishlistRepository wishlistRepository,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<RemoveWishlistItemCommand>
{
    public async Task<Result> Handle(RemoveWishlistItemCommand request, CancellationToken cancellationToken)
    {
        Result<Wishlist> wishlist = await wishlistRepository.GetOwnAsync(
            request.WishlistId,
            userIdentifierProvider.UserId,
            cancellationToken);

        if (wishlist.IsFailure)
        {
            return wishlist;
        }

        wishlist.Value.RemoveItem(request.ApartmentId, dateTimeProvider.UtcNow);

        return Result.Success();
    }
}
