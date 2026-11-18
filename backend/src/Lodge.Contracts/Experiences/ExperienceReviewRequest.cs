namespace Lodge.Contracts.Experiences;

/// <summary>
/// Reviews a completed experience (<c>POST /experience-bookings/{id}/review</c>).
/// </summary>
public sealed record ExperienceReviewRequest(int Rating, string Comment);
