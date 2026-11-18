using Lodge.Domain.Core.Abstractions;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Wishlists;

/// <summary>
/// A named list of saved listings. Only its owner can see or change it.
/// </summary>
public sealed class Wishlist : Entity, IAuditableEntity
{
    public const int NameMaxLength = 50;

    /// <summary>
    /// The maximum number of wishlists per user.
    /// </summary>
    public const int MaxPerUser = 50;

    private readonly List<WishlistItem> _items = [];

    private Wishlist(Guid id, Guid userId, string name, DateTime utcNow)
        : base(id)
    {
        UserId = userId;
        Name = name;
        UpdatedOnUtc = utcNow;
    }

    private Wishlist()
    {
    }

    public Guid UserId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public IReadOnlyCollection<WishlistItem> Items => _items;

    /// <summary>
    /// Gets when the wishlist (name or items) last changed.
    /// </summary>
    public DateTime UpdatedOnUtc { get; private set; }

    public DateTime CreatedOnUtc { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public static Result<Wishlist> Create(Guid userId, string? name, DateTime utcNow)
    {
        Result<string> nameResult = ValidateName(name);

        if (nameResult.IsFailure)
        {
            return Result.Failure<Wishlist>(nameResult.Error);
        }

        return new Wishlist(Guid.NewGuid(), userId, nameResult.Value, utcNow);
    }

    public Result Rename(string? name, DateTime utcNow)
    {
        Result<string> nameResult = ValidateName(name);

        if (nameResult.IsFailure)
        {
            return nameResult;
        }

        Name = nameResult.Value;
        UpdatedOnUtc = utcNow;

        return Result.Success();
    }

    /// <summary>
    /// Adds the apartment. Does nothing when it is already in the list.
    /// </summary>
    public void AddItem(Guid apartmentId, DateTime utcNow)
    {
        if (_items.Exists(item => item.ApartmentId == apartmentId))
        {
            return;
        }

        _items.Add(new WishlistItem(apartmentId, utcNow));
        UpdatedOnUtc = utcNow;
    }

    /// <summary>
    /// Removes the apartment. Does nothing when it is not in the list.
    /// </summary>
    public void RemoveItem(Guid apartmentId, DateTime utcNow)
    {
        if (_items.RemoveAll(item => item.ApartmentId == apartmentId) > 0)
        {
            UpdatedOnUtc = utcNow;
        }
    }

    private static Result<string> ValidateName(string? name)
    {
        string trimmed = name?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            return Result.Failure<string>(WishlistErrors.NameRequired);
        }

        if (trimmed.Length > NameMaxLength)
        {
            return Result.Failure<string>(WishlistErrors.NameTooLong);
        }

        return trimmed;
    }
}
