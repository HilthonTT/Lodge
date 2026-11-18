namespace Lodge.Contracts.Experiences;

public sealed record ExperienceHostResponse(Guid Id, string FirstName, string? AvatarUrl, bool IsSuperhost);
