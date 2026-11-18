using Lodge.Application.Abstractions.App;
using Lodge.Application.Abstractions.Storage;
using Lodge.Application.Core.Extensions;
using Lodge.Contracts.Experiences;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Experiences;

namespace Lodge.Application.Experiences;

/// <summary>
/// Turns experience requests into domain values and experiences into responses.
/// </summary>
internal static class ExperienceMapper
{
    public static Result<ExperienceDetails> ToDetails(ExperienceRequest request)
    {
        if (!EnumNames.TryParse(request.Category, out ExperienceCategory category))
        {
            return Result.Failure<ExperienceDetails>(ExperienceErrors.InvalidCategory);
        }

        return new ExperienceDetails
        {
            Title = request.Title ?? string.Empty,
            Description = request.Description ?? string.Empty,
            Category = category,
            City = request.City ?? string.Empty,
            Country = request.Country ?? string.Empty,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            MeetingPoint = request.MeetingPoint ?? string.Empty,
            DurationMinutes = request.DurationMinutes,
            PricePerGuest = request.PricePerGuest,
            Currency = request.Currency ?? string.Empty,
            MaxGuests = request.MaxGuests,
            Languages = request.Languages ?? [],
            Included = request.Included ?? [],
        };
    }

    /// <summary>
    /// Resolves the photo ids: photos already on the experience are kept, other ids must be files uploaded by the user
    /// (through <c>POST /hosting/photos</c>).
    /// </summary>
    public static async Task<Result<List<Guid>>> ResolveImagesAsync(
        IBlobService blobService,
        Experience? experience,
        IReadOnlyList<Guid>? imageIds,
        Guid userId,
        CancellationToken cancellationToken)
    {
        List<Guid> images = [];

        foreach (Guid imageId in imageIds ?? [])
        {
            if (experience?.HasImage(imageId) == true)
            {
                images.Add(imageId);
                continue;
            }

            if (imageId == Guid.Empty || !await blobService.IsUserOwnerAsync(imageId, userId, cancellationToken))
            {
                return Result.Failure<List<Guid>>(ExperienceErrors.InvalidImage(imageId));
            }

            images.Add(imageId);
        }

        return images;
    }

    public static List<ExperienceImageResponse> Images(IEnumerable<Guid> imageIds, AppOptions appOptions) =>
        [.. imageIds.Select(id => new ExperienceImageResponse(id, appOptions.FileUrl(id)))];
}
