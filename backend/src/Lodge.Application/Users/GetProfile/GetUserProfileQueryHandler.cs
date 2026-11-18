using System.Data;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Apartments;
using Lodge.Contracts.Apartments;
using Lodge.Contracts.Users;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Lodge.Application.Users.GetProfile;

internal sealed class GetUserProfileQueryHandler(IDbConnectionFactory factory, ApartmentReader apartmentReader)
    : IQueryHandler<GetUserProfileQuery, UserProfileResponse>
{
    /// <summary>
    /// The maximum number of listings on a profile.
    /// </summary>
    public const int MaxListings = 12;

    /// <summary>
    /// The maximum number of guest reviews on a profile.
    /// </summary>
    public const int MaxGuestReviews = 10;

    public async Task<Result<UserProfileResponse>> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        UserQueries.ProfileRow? row = await UserQueries.GetProfileRowAsync(connection, request.UserId);

        if (row is null)
        {
            return Result.Failure<UserProfileResponse>(UserErrors.NotFound(request.UserId));
        }

        List<ApartmentResponse> listings = await apartmentReader.QueryAsync(
            connection,
            $"""
            WHERE a.owner_id = @UserId AND {ApartmentReader.ListedCondition}
            ORDER BY a.created_on_utc DESC, a.id
            LIMIT {MaxListings}
            """,
            new { request.UserId });

        List<UserQueries.GuestReviewRow> guestReviews =
            await UserQueries.GetGuestReviewsAsync(connection, request.UserId, MaxGuestReviews);

        return new UserProfileResponse(
            row.Id,
            row.FirstName,
            row.LastName.Length > 0 ? char.ToUpperInvariant(row.LastName[0]).ToString() : string.Empty,
            apartmentReader.AvatarUrl(row.ImageId),
            row.Bio,
            row.JoinedOnUtc,
            row.EmailVerified,
            row.ReviewsWrittenCount,
            row.IsSuperhost,
            row.HostingSince,
            listings,
            row.HostReviewCount,
            row.HostAverageRating,
            row.GuestReviewCount,
            row.GuestAverageRating,
            [.. guestReviews.Select(review => new GuestReviewResponse(
                review.Id,
                review.Rating,
                review.Comment,
                review.HostFirstName,
                apartmentReader.AvatarUrl(review.HostImageId),
                review.ListingName,
                review.CreatedOnUtc))]);
    }
}
