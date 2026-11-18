namespace Lodge.Contracts.Experiences;

/// <summary>
/// A review of an experience (<c>GET /experiences/{id}/reviews</c>).
/// </summary>
public sealed record ExperienceReviewResponse(
    Guid Id,
    int Rating,
    string Comment,
    DateTime CreatedOnUtc,
    ExperienceReviewerResponse Guest);
