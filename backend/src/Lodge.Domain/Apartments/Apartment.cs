using Lodge.Domain.Apartments.Events;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Abstractions;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Shared;

namespace Lodge.Domain.Apartments;

/// <summary>
/// Represents a listing (historically called "apartment") owned by a host.
/// </summary>
public sealed class Apartment : Entity, IAuditableEntity, ISoftDeletableEntity
{
    public const int MaxImages = 20;
    public const int TextMaxLength = 2000;
    public const int MaxDiscountPercent = 90;
    public const int MaxNightsLimit = 365;
    public const int MaxCalendarDays = 366;
    public const int MaxBedrooms = 50;
    public const int MaxBeds = 100;
    public const int MaxBathrooms = 50;
    public const int MaxGuests = 50;

    /// <summary>
    /// Stays of at least this many nights get the weekly discount.
    /// </summary>
    public const int WeeklyDiscountMinNights = 7;

    /// <summary>
    /// Stays of at least this many nights get the monthly discount (instead of the weekly one).
    /// </summary>
    public const int MonthlyDiscountMinNights = 28;

    private readonly List<ApartmentImage> _images = [];

    private Apartment(Guid id, Guid ownerId)
        : base(id)
    {
        OwnerId = ownerId;
    }

    /// <remarks>
    /// Required by EF Core.
    /// </remarks>
    private Apartment()
    {
    }

    /// <summary>
    /// Gets the identifier of the host (user) who owns the listing.
    /// </summary>
    public Guid OwnerId { get; private set; }

    public Name Name { get; private set; } = null!;

    public Description Description { get; private set; } = null!;

    public PropertyType PropertyType { get; private set; }

    public Address Address { get; private set; } = null!;

    public double Latitude { get; private set; }

    public double Longitude { get; private set; }

    /// <summary>
    /// Gets the base nightly price.
    /// </summary>
    public Money Price { get; private set; } = null!;

    public Money CleaningFee { get; private set; } = null!;

    /// <summary>
    /// Gets the nightly price of Friday and Saturday nights, in the currency of <see cref="Price"/>, or null.
    /// </summary>
    public decimal? WeekendPrice { get; private set; }

    public int WeeklyDiscountPercent { get; private set; }

    public int MonthlyDiscountPercent { get; private set; }

    public int Bedrooms { get; private set; }

    public int Beds { get; private set; }

    public decimal Bathrooms { get; private set; }

    public int MaximumGuestCount { get; private set; }

    public List<Amenity> Amenities { get; private set; } = [];

    public TimeOnly CheckInTime { get; private set; }

    public TimeOnly CheckOutTime { get; private set; }

    public bool PetsAllowed { get; private set; }

    public bool SmokingAllowed { get; private set; }

    public bool PartiesAllowed { get; private set; }

    public string? HouseRules { get; private set; }

    public string? CheckInInstructions { get; private set; }

    /// <summary>
    /// Gets a value indicating whether guests can book without the host's approval.
    /// </summary>
    public bool InstantBook { get; private set; }

    /// <summary>
    /// Gets the cancellation policy (snapshotted on every booking).
    /// </summary>
    public CancellationPolicy CancellationPolicy { get; private set; } = CancellationPolicy.Moderate;

    public int MinNights { get; private set; }

    public int MaxNights { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the listing is visible to guests.
    /// </summary>
    public bool IsListed { get; private set; }

    /// <summary>
    /// Gets the photos (unordered: use <see cref="OrderedImages"/> for the display order).
    /// </summary>
    public IReadOnlyCollection<ApartmentImage> Images => _images;

    public DateTime? LastBookedOnUtc { get; internal set; }

    public DateTime CreatedOnUtc { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public DateTime? DeletedOnUtc { get; set; }

    public bool Deleted { get; set; }

    /// <summary>
    /// Creates a new listing.
    /// </summary>
    public static Result<Apartment> Create(
        Guid ownerId,
        ListingDetails details,
        IReadOnlyList<ApartmentImage> images,
        bool isListed = true)
    {
        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("The owner is required.", nameof(ownerId));
        }

        var apartment = new Apartment(Guid.NewGuid(), ownerId) { IsListed = isListed };

        Result result = apartment.Update(details);

        if (result.IsFailure)
        {
            return Result.Failure<Apartment>(result.Error);
        }

        result = apartment.SetImages(images);

        if (result.IsFailure)
        {
            return Result.Failure<Apartment>(result.Error);
        }

        apartment.RaiseDomainEvent(new ApartmentCreatedDomainEvent(apartment.Id));

        return apartment;
    }

    /// <summary>
    /// Replaces the editable details of the listing.
    /// </summary>
    public Result Update(ListingDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);

        Result validation = Validate(details);

        if (validation.IsFailure)
        {
            return validation;
        }

        Name = details.Name;
        Description = details.Description;
        PropertyType = details.PropertyType;
        Address = details.Address;
        Latitude = details.Latitude;
        Longitude = details.Longitude;
        // Money is an owned type: owned instances must not be shared with other entities.
        Price = details.Price with { };
        CleaningFee = details.CleaningFee with { };
        WeekendPrice = details.WeekendPrice;
        WeeklyDiscountPercent = details.WeeklyDiscountPercent;
        MonthlyDiscountPercent = details.MonthlyDiscountPercent;
        Bedrooms = details.Bedrooms;
        Beds = details.Beds;
        Bathrooms = details.Bathrooms;
        MaximumGuestCount = details.MaximumGuestCount;
        Amenities = [.. details.Amenities.Distinct().Order()];
        CheckInTime = details.CheckInTime;
        CheckOutTime = details.CheckOutTime;
        PetsAllowed = details.PetsAllowed;
        SmokingAllowed = details.SmokingAllowed;
        PartiesAllowed = details.PartiesAllowed;
        HouseRules = Normalize(details.HouseRules);
        CheckInInstructions = Normalize(details.CheckInInstructions);
        InstantBook = details.InstantBook;
        CancellationPolicy = details.CancellationPolicy;
        MinNights = details.MinNights;
        MaxNights = details.MaxNights;

        return Result.Success();
    }

    /// <summary>
    /// Sets the photos in display order. Photos that are already on the listing (same <see cref="ApartmentImage.Id"/>)
    /// are kept and re-ordered, the others are added, and missing ones are removed.
    /// </summary>
    public Result SetImages(IReadOnlyList<ApartmentImage> images)
    {
        ArgumentNullException.ThrowIfNull(images);

        if (images.Count is 0 or > MaxImages)
        {
            return Result.Failure(ApartmentErrors.InvalidImageCount);
        }

        if (images.Select(image => image.Id).Distinct().Count() != images.Count)
        {
            return Result.Failure(ApartmentErrors.DuplicateImage);
        }

        _images.RemoveAll(existing => images.All(image => image.Id != existing.Id));

        for (int position = 0; position < images.Count; position++)
        {
            ApartmentImage? existing = _images.Find(image => image.Id == images[position].Id);

            if (existing is null)
            {
                images[position].Position = position;
                _images.Add(images[position]);
            }
            else
            {
                existing.Position = position;
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// Gets the photos in display order (the cover photo first).
    /// </summary>
    public IReadOnlyList<ApartmentImage> OrderedImages() => [.. _images.OrderBy(image => image.Position)];

    public bool HasImage(Guid imageId) => _images.Exists(image => image.Id == imageId);

    public void SetListed(bool isListed) => IsListed = isListed;

    public void ChangeOwner(Guid ownerId)
    {
        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("The owner is required.", nameof(ownerId));
        }

        OwnerId = ownerId;
    }

    public bool IsOwnedBy(Guid userId) => OwnerId == userId;

    /// <summary>
    /// Soft-deletes the listing. It is unlisted, but its bookings and reviews are kept.
    /// </summary>
    public void Delete(DateTime utcNow)
    {
        IsListed = false;
        Deleted = true;
        DeletedOnUtc = utcNow;
    }

    /// <summary>
    /// Gets the price of the night that starts on <paramref name="night"/>: the host's override for that date, else the
    /// weekend price for Friday and Saturday nights (when set), else the base price.
    /// </summary>
    public decimal GetNightlyPrice(DateOnly night, decimal? overridePrice = null)
    {
        if (overridePrice is decimal price)
        {
            return price;
        }

        if (WeekendPrice is decimal weekendPrice && IsWeekendNight(night))
        {
            return weekendPrice;
        }

        return Price.Amount;
    }

    /// <summary>
    /// Checks the number of nights against <see cref="MinNights"/> and <see cref="MaxNights"/>.
    /// </summary>
    public Result EnsureStayLength(int nights)
    {
        if (nights < MinNights)
        {
            return Result.Failure(BookingErrors.StayTooShort(MinNights));
        }

        if (nights > MaxNights)
        {
            return Result.Failure(BookingErrors.StayTooLong(MaxNights));
        }

        return Result.Success();
    }

    /// <summary>
    /// Friday and Saturday nights are weekend nights.
    /// </summary>
    public static bool IsWeekendNight(DateOnly night) => night.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday;

    private static Result Validate(ListingDetails details)
    {
        if (details.Latitude is < -90 or > 90 || details.Longitude is < -180 or > 180 ||
            double.IsNaN(details.Latitude) || double.IsNaN(details.Longitude))
        {
            return Result.Failure(ApartmentErrors.InvalidCoordinates);
        }

        if (details.Price.Amount <= 0 ||
            details.CleaningFee.Amount < 0 ||
            details.CleaningFee.Currency != details.Price.Currency ||
            details.WeekendPrice is <= 0)
        {
            return Result.Failure(ApartmentErrors.InvalidPrice);
        }

        if (details.WeeklyDiscountPercent is < 0 or > MaxDiscountPercent ||
            details.MonthlyDiscountPercent is < 0 or > MaxDiscountPercent)
        {
            return Result.Failure(ApartmentErrors.InvalidDiscount);
        }

        if (details.Bedrooms is < 0 or > MaxBedrooms ||
            details.Beds is < 1 or > MaxBeds ||
            details.Bathrooms is < 0 or > MaxBathrooms ||
            details.Bathrooms % 0.5m != 0 ||
            details.MaximumGuestCount is < 1 or > MaxGuests)
        {
            return Result.Failure(ApartmentErrors.InvalidCapacity);
        }

        if (details.MinNights < 1 || details.MaxNights > MaxNightsLimit || details.MinNights > details.MaxNights)
        {
            return Result.Failure(ApartmentErrors.InvalidStayLimits);
        }

        if (!Enum.IsDefined(details.CancellationPolicy))
        {
            return Result.Failure(ApartmentErrors.InvalidCancellationPolicy);
        }

        if (details.HouseRules?.Trim().Length > TextMaxLength)

        {
            return Result.Failure(ApartmentErrors.HouseRulesTooLong);
        }

        if (details.CheckInInstructions?.Trim().Length > TextMaxLength)
        {
            return Result.Failure(ApartmentErrors.CheckInInstructionsTooLong);
        }

        return Result.Success();
    }

    private static string? Normalize(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
