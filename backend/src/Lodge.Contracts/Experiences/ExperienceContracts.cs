namespace Lodge.Contracts.Experiences;

/// <summary>
/// Creates or updates an experience (<c>POST</c>/<c>PUT /hosting/experiences</c>).
/// </summary>
public sealed record ExperienceRequest
{
    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets <c>Food</c>, <c>Outdoors</c>, <c>Art</c>, <c>Culture</c>, <c>Sports</c>, <c>Wellness</c>, <c>Nightlife</c>
    /// or <c>Workshops</c>.
    /// </summary>
    public string Category { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public string MeetingPoint { get; init; } = string.Empty;

    public int DurationMinutes { get; init; }

    public decimal PricePerGuest { get; init; }

    /// <summary>
    /// Gets <c>USD</c> or <c>EUR</c>.
    /// </summary>
    public string Currency { get; init; } = "USD";

    public int MaxGuests { get; init; }

    /// <summary>
    /// Gets the ISO 639-1 language codes (e.g. <c>["en", "nl"]</c>).
    /// </summary>
    public IReadOnlyList<string> Languages { get; init; } = [];

    public IReadOnlyList<string> Included { get; init; } = [];

    /// <summary>
    /// Gets the photo ids (from <c>POST /hosting/photos</c>), cover photo first; 0 to 20.
    /// </summary>
    public IReadOnlyList<Guid> ImageIds { get; init; } = [];

    /// <summary>
    /// Gets whether the experience is listed (create only; defaults to true).
    /// </summary>
    public bool? IsListed { get; init; }
}

public sealed record UpdateExperienceStatusRequest(bool IsListed);

public sealed record ExperienceImageResponse(Guid Id, string Url);

public sealed record ExperienceHostResponse(Guid Id, string FirstName, string? AvatarUrl, bool IsSuperhost);

/// <summary>
/// An experience in search results.
/// </summary>
public sealed record ExperienceSummaryResponse
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public int DurationMinutes { get; init; }

    public decimal PricePerGuest { get; init; }

    public string Currency { get; init; } = string.Empty;

    /// <summary>
    /// Gets the cover photo URL, or null (the frontend shows a category placeholder).
    /// </summary>
    public string? ImageUrl { get; init; }

    public IReadOnlyList<string> Languages { get; init; } = [];

    public decimal? AverageRating { get; init; }

    public int ReviewCount { get; init; }

    /// <summary>
    /// Gets the start of the next session with enough spots (on the searched date, if any).
    /// </summary>
    public DateTime? NextSessionStartsAtUtc { get; init; }

    public Guid HostId { get; init; }

    public string HostFirstName { get; init; } = string.Empty;
}

/// <summary>
/// The full experience (<c>GET /experiences/{id}</c> and <c>GET /hosting/experiences/{id}</c>).
/// </summary>
public sealed record ExperienceResponse
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public string MeetingPoint { get; init; } = string.Empty;

    public int DurationMinutes { get; init; }

    public decimal PricePerGuest { get; init; }

    public string Currency { get; init; } = string.Empty;

    public int MaxGuests { get; init; }

    public IReadOnlyList<string> Languages { get; init; } = [];

    public IReadOnlyList<string> Included { get; init; } = [];

    public IReadOnlyList<ExperienceImageResponse> Images { get; init; } = [];

    public ExperienceHostResponse Host { get; init; } = null!;

    public decimal? AverageRating { get; init; }

    public int ReviewCount { get; init; }

    public bool IsListed { get; init; }

    /// <summary>
    /// Gets how many hours before the start guests can cancel with a full refund (24).
    /// </summary>
    public int FreeCancellationHours { get; init; }

    public DateTime CreatedOnUtc { get; init; }
}

/// <summary>
/// A bookable session (<c>GET /experiences/{id}/sessions</c>).
/// </summary>
public sealed record ExperienceSessionResponse(Guid Id, DateTime StartsAtUtc, int Capacity, int SpotsLeft);

/// <summary>
/// A session as the host sees it (<c>GET /hosting/experiences/{id}/sessions</c>).
/// </summary>
public sealed record HostExperienceSessionResponse
{
    public Guid Id { get; init; }

    public DateTime StartsAtUtc { get; init; }

    public int Capacity { get; init; }

    public int BookedGuests { get; init; }

    public int SpotsLeft { get; init; }

    /// <summary>
    /// Gets the number of reserved and confirmed bookings.
    /// </summary>
    public int BookingCount { get; init; }

    public bool IsCancelled { get; init; }
}

/// <summary>
/// The host's experience list item (<c>GET /hosting/experiences</c>).
/// </summary>
public sealed record HostExperienceSummaryResponse
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;

    public decimal PricePerGuest { get; init; }

    public string Currency { get; init; } = string.Empty;

    public bool IsListed { get; init; }

    public string? ImageUrl { get; init; }

    public int UpcomingSessionCount { get; init; }

    public decimal? AverageRating { get; init; }

    public int ReviewCount { get; init; }

    public DateTime CreatedOnUtc { get; init; }
}

/// <summary>
/// Creates one session (<see cref="StartsAtUtc"/> + <see cref="Capacity"/>) or a recurring schedule
/// (<see cref="Recurring"/>). The capacity defaults to the experience's maximum number of guests.
/// </summary>
public sealed record CreateSessionsRequest
{
    public DateTime? StartsAtUtc { get; init; }

    public int? Capacity { get; init; }

    public RecurringSessionsRequest? Recurring { get; init; }
}

/// <summary>
/// A weekly schedule: one session on each of <see cref="DaysOfWeek"/> (e.g. <c>"Saturday"</c>) at
/// <see cref="StartTime"/> (<c>HH:mm</c>, local time in <see cref="TimeZone"/>, an IANA id) from <see cref="From"/> to
/// <see cref="Until"/> (inclusive). At most 200 sessions; past times and existing sessions are skipped.
/// </summary>
public sealed record RecurringSessionsRequest
{
    public IReadOnlyList<string> DaysOfWeek { get; init; } = [];

    public string StartTime { get; init; } = string.Empty;

    public string TimeZone { get; init; } = string.Empty;

    public DateOnly From { get; init; }

    public DateOnly Until { get; init; }
}

/// <summary>
/// The ids of the created sessions.
/// </summary>
public sealed record CreatedSessionsResponse(IReadOnlyList<Guid> Ids);

public sealed record ExperienceReviewerResponse(Guid Id, string FirstName, string? AvatarUrl);

/// <summary>
/// A review of an experience (<c>GET /experiences/{id}/reviews</c>).
/// </summary>
public sealed record ExperienceReviewResponse(
    Guid Id,
    int Rating,
    string Comment,
    DateTime CreatedOnUtc,
    ExperienceReviewerResponse Guest);

/// <summary>
/// Books spots in a session (<c>POST /experience-bookings</c>, with an <c>X-Idempotency-Key</c> header).
/// </summary>
public sealed record ReserveExperienceRequest(Guid SessionId, int GuestCount);

/// <summary>
/// Reviews a completed experience (<c>POST /experience-bookings/{id}/review</c>).
/// </summary>
public sealed record ExperienceReviewRequest(int Rating, string Comment);

/// <summary>
/// An experience booking (guest and host views).
/// </summary>
public sealed record ExperienceBookingResponse
{
    public Guid Id { get; init; }

    public Guid ExperienceId { get; init; }

    public string ExperienceTitle { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string? ImageUrl { get; init; }

    public string City { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;

    public string MeetingPoint { get; init; } = string.Empty;

    public int DurationMinutes { get; init; }

    public Guid SessionId { get; init; }

    public DateTime StartsAtUtc { get; init; }

    public Guid HostId { get; init; }

    public string HostFirstName { get; init; } = string.Empty;

    public Guid GuestId { get; init; }

    public string GuestName { get; init; } = string.Empty;

    public int GuestCount { get; init; }

    /// <summary>
    /// Gets the price per guest × guests.
    /// </summary>
    public decimal PriceTotal { get; init; }

    public decimal ServiceFee { get; init; }

    /// <summary>
    /// Gets the amount charged: <see cref="PriceTotal"/> + <see cref="ServiceFee"/>.
    /// </summary>
    public decimal TotalPrice { get; init; }

    public string Currency { get; init; } = string.Empty;

    /// <summary>
    /// Gets <c>Reserved</c>, <c>Confirmed</c>, <c>Cancelled</c> or <c>Completed</c>.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    public decimal? AmountPaid { get; init; }

    public DateTime? PaidOnUtc { get; init; }

    public decimal? RefundAmount { get; init; }

    /// <summary>
    /// Gets <c>None</c>, <c>Pending</c>, <c>Succeeded</c>, <c>Failed</c> or <c>Manual</c>.
    /// </summary>
    public string RefundStatus { get; init; } = "None";

    public bool CancelledByHost { get; init; }

    public DateTime? CancelledOnUtc { get; init; }

    /// <summary>
    /// Gets the last moment for a full refund (start − 24 h).
    /// </summary>
    public DateTime FreeCancellationUntilUtc { get; init; }

    public bool CanCancel { get; init; }

    public bool CanReview { get; init; }

    public bool HasReview { get; init; }

    public DateTime CreatedOnUtc { get; init; }
}
