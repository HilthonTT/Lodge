namespace Lodge.Domain.Experiences;

/// <summary>
/// The category of an experience (stored and serialized by name).
/// </summary>
public enum ExperienceCategory
{
    Food = 1,
    Outdoors = 2,
    Art = 3,
    Culture = 4,
    Sports = 5,
    Wellness = 6,
    Nightlife = 7,
    Workshops = 8,
}

/// <summary>
/// The status of an experience booking (stored and serialized by name).
/// </summary>
public enum ExperienceBookingStatus
{
    /// <summary>
    /// The spots are held and the guest has to pay (unpaid holds are released after a while).
    /// </summary>
    Reserved = 1,

    Confirmed = 2,

    Cancelled = 3,

    /// <summary>
    /// The session took place.
    /// </summary>
    Completed = 4,
}
