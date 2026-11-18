using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.Reviews;

/// <summary>
/// The category ratings of a guest's review of a listing (each 1 to 5).
/// </summary>
public sealed record CategoryRatings
{
    private CategoryRatings(int cleanliness, int accuracy, int checkIn, int communication, int location, int value)
    {
        Cleanliness = cleanliness;
        Accuracy = accuracy;
        CheckIn = checkIn;
        Communication = communication;
        Location = location;
        Value = value;
    }

    /// <remarks>
    /// Required by EF Core.
    /// </remarks>
    private CategoryRatings()
    {
    }

    public int Cleanliness { get; private init; }

    public int Accuracy { get; private init; }

    public int CheckIn { get; private init; }

    public int Communication { get; private init; }

    public int Location { get; private init; }

    public int Value { get; private init; }

    public static Result<CategoryRatings> Create(
        int cleanliness,
        int accuracy,
        int checkIn,
        int communication,
        int location,
        int value)
    {
        int[] ratings = [cleanliness, accuracy, checkIn, communication, location, value];

        if (ratings.Any(rating => rating is < 1 or > 5))
        {
            return Result.Failure<CategoryRatings>(ReviewErrors.InvalidCategoryRating);
        }

        return new CategoryRatings(cleanliness, accuracy, checkIn, communication, location, value);
    }

    /// <summary>
    /// The same rating for every category (used for reviews written before categories existed).
    /// </summary>
    public static CategoryRatings Uniform(Rating rating)
    {
        ArgumentNullException.ThrowIfNull(rating);

        return new CategoryRatings(rating.Value, rating.Value, rating.Value, rating.Value, rating.Value, rating.Value);
    }
}
