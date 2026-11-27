using Lodge.Application.Abstractions.Messaging;

namespace Lodge.Application.Experiences.CreateReview;

/// <summary>
/// The guest reviews a completed experience booking, once (published right away).
/// </summary>
public sealed record CreateExperienceReviewCommand(Guid ExperienceBookingId, int Rating, string Comment) : ICommand<Guid>;
