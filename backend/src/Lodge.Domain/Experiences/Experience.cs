using System.Text.RegularExpressions;
using Lodge.Domain.Core.Abstractions;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Experiences;

/// <summary>
/// An activity hosted by a local host (Airbnb Experiences): it takes place in <see cref="ExperienceSession"/>s that
/// guests book per person.
/// </summary>
/// <remarks>
/// Cancellation policy: a guest who cancels at least <see cref="FreeCancellationHours"/> hours before the session
/// starts gets a full refund, otherwise none. Hosts who cancel a session refund every guest in full.
/// </remarks>
public sealed partial class Experience : Entity, IAuditableEntity, ISoftDeletableEntity
{
    public const int TitleMaxLength = 120;
    public const int DescriptionMaxLength = 4000;
    public const int MeetingPointMaxLength = 500;
    public const int PlaceMaxLength = 100;
    public const int MinDurationMinutes = 30;
    public const int MaxDurationMinutes = 1440;
    public const int MaxGuestsLimit = 50;
    public const int MaxImages = 20;
    public const int MaxLanguages = 10;
    public const int MaxIncludedItems = 20;
    public const int IncludedItemMaxLength = 100;
    public const decimal MaxPricePerGuest = 100_000m;

    /// <summary>
    /// Guests who cancel at least this many hours before the session get a full refund.
    /// </summary>
    public const int FreeCancellationHours = 24;

    private Experience(Guid id, Guid hostId)
        : base(id)
    {
        HostId = hostId;
    }

    /// <remarks>
    /// Required by EF Core.
    /// </remarks>
    private Experience()
    {
    }

    public Guid HostId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public ExperienceCategory Category { get; private set; }

    public string City { get; private set; } = string.Empty;

    public string Country { get; private set; } = string.Empty;

    public double Latitude { get; private set; }

    public double Longitude { get; private set; }

    public string MeetingPoint { get; private set; } = string.Empty;

    public int DurationMinutes { get; private set; }

    public decimal PricePerGuest { get; private set; }

    public string Currency { get; private set; } = "USD";

    public int MaxGuests { get; private set; }

    public List<string> Languages { get; private set; } = [];

    public List<string> Included { get; private set; } = [];

    /// <summary>
    /// Gets the uploaded photo (file) ids in display order; the first one is the cover photo.
    /// </summary>
    public List<Guid> ImageIds { get; private set; } = [];

    public bool IsListed { get; private set; }

    public DateTime CreatedOnUtc { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public DateTime? DeletedOnUtc { get; set; }

    public bool Deleted { get; set; }

    public static Result<Experience> Create(
        Guid hostId,
        ExperienceDetails details,
        IReadOnlyList<Guid> imageIds,
        bool isListed = true)
    {
        var experience = new Experience(Guid.NewGuid(), hostId);

        Result result = experience.Update(details, imageIds);

        if (result.IsFailure)
        {
            return Result.Failure<Experience>(result.Error);
        }

        experience.IsListed = isListed;

        return experience;
    }

    /// <summary>
    /// Validates the details and the photos and applies them.
    /// </summary>
    public Result Update(ExperienceDetails details, IReadOnlyList<Guid> imageIds)
    {
        ArgumentNullException.ThrowIfNull(details);
        ArgumentNullException.ThrowIfNull(imageIds);

        Result valid = Validate(details);

        if (valid.IsFailure)
        {
            return valid;
        }

        if (imageIds.Count > MaxImages)
        {
            return Result.Failure(ExperienceErrors.TooManyImages);
        }

        Title = details.Title.Trim();
        Description = details.Description.Trim();
        Category = details.Category;
        City = details.City.Trim();
        Country = details.Country.Trim();
        Latitude = details.Latitude;
        Longitude = details.Longitude;
        MeetingPoint = details.MeetingPoint.Trim();
        DurationMinutes = details.DurationMinutes;
        PricePerGuest = Math.Round(details.PricePerGuest, 2, MidpointRounding.AwayFromZero);
        Currency = details.Currency.Trim().ToUpperInvariant();
        MaxGuests = details.MaxGuests;
        Languages = [.. details.Languages.Select(language => language.Trim().ToLowerInvariant()).Distinct()];
        Included = [.. details.Included.Select(item => item.Trim()).Where(item => item.Length > 0)];
        ImageIds = [.. imageIds.Distinct()];

        return Result.Success();
    }

    public void SetListed(bool isListed) => IsListed = isListed;

    public bool IsHostedBy(Guid userId) => HostId == userId;

    public bool HasImage(Guid imageId) => ImageIds.Contains(imageId);

    /// <summary>
    /// Gets the price of <paramref name="guestCount"/> guests (before the service fee).
    /// </summary>
    public decimal PriceFor(int guestCount) => PricePerGuest * guestCount;

    private static Result Validate(ExperienceDetails details)
    {
        static bool Within(string? value, int maxLength) =>
            !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= maxLength;

        if (!Within(details.Title, TitleMaxLength))
        {
            return Result.Failure(ExperienceErrors.InvalidTitle);
        }

        if (!Within(details.Description, DescriptionMaxLength))
        {
            return Result.Failure(ExperienceErrors.InvalidDescription);
        }

        if (!Enum.IsDefined(details.Category))
        {
            return Result.Failure(ExperienceErrors.InvalidCategory);
        }

        if (!Within(details.City, PlaceMaxLength) ||
            !Within(details.Country, PlaceMaxLength) ||
            details.Latitude is < -90 or > 90 ||
            details.Longitude is < -180 or > 180)
        {
            return Result.Failure(ExperienceErrors.InvalidLocation);
        }

        if (!Within(details.MeetingPoint, MeetingPointMaxLength))
        {
            return Result.Failure(ExperienceErrors.InvalidMeetingPoint);
        }

        if (details.DurationMinutes is < MinDurationMinutes or > MaxDurationMinutes)
        {
            return Result.Failure(ExperienceErrors.InvalidDuration);
        }

        if (details.PricePerGuest <= 0 || details.PricePerGuest > MaxPricePerGuest)
        {
            return Result.Failure(ExperienceErrors.InvalidPrice);
        }

        if (!Shared.Currency.All.Any(currency =>
                string.Equals(currency.Code, details.Currency?.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure(ExperienceErrors.InvalidCurrency);
        }

        if (details.MaxGuests is < 1 or > MaxGuestsLimit)
        {
            return Result.Failure(ExperienceErrors.InvalidMaxGuests);
        }

        if (details.Languages is null ||
            details.Languages.Count is 0 or > MaxLanguages ||
            details.Languages.Any(language => language is null || !LanguageCode().IsMatch(language.Trim())))
        {
            return Result.Failure(ExperienceErrors.InvalidLanguages);
        }

        if (details.Included is null ||
            details.Included.Count > MaxIncludedItems ||
            details.Included.Any(item => item is null || item.Trim().Length > IncludedItemMaxLength))
        {
            return Result.Failure(ExperienceErrors.InvalidIncluded);
        }

        return Result.Success();
    }

    [GeneratedRegex("^[A-Za-z]{2}$")]
    private static partial Regex LanguageCode();
}
