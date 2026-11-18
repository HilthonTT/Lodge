using System.Net;
using System.Net.Http.Json;
using Lodge.Api.FunctionalTests.Abstractions;
using Lodge.Application.Reviews.Publish;
using Lodge.Contracts.Apartments;
using Lodge.Contracts.Bookings;
using Lodge.Contracts.Common;
using Lodge.Contracts.Reviews;
using Lodge.Contracts.Users;

namespace Lodge.Api.FunctionalTests.Reviews;

/// <summary>
/// Category ratings, the review window, double-blind publishing, host replies and host-to-guest reviews.
/// </summary>
public class ReviewFlowTests(FunctionalTestWebAppFactory factory) : HostingFunctionalTest(factory)
{
    private static readonly CategoryRatingsDto Categories = new()
    {
        Cleanliness = 5,
        Accuracy = 4,
        CheckIn = 5,
        Communication = 5,
        Location = 3,
        Value = 4,
    };

    [Fact]
    public async Task Reviews_Should_BeDoubleBlind_UntilBothSidesHaveReviewed()
    {
        (HttpClient host, _, Guid listingId) = await CreateHostWithListingAsync();
        (HttpClient guest, Guid guestId) = await CreateAuthenticatedUserAsync();
        Guid bookingId = await CreateCompletedStayAsync(listingId, guestId, checkedOutDaysAgo: 2);

        BookingResponse forGuest = await GetBookingAsync(guest, bookingId);
        forGuest.Should().BeEquivalentTo(new { CanReview = true, HasReview = false, HasGuestReview = false });
        forGuest.ReviewDeadline.Should().Be(DateOnly.FromDateTime(forGuest.DurationEnd).AddDays(14));
        (await GetBookingAsync(host, bookingId)).CanReview.Should().BeTrue();

        Guid reviewId = await PostReviewAsync(guest, guestId, bookingId, 4, Categories);

        // Only the author sees the unpublished review; the listing is not rated yet.
        (await GetReviewsAsync(HttpClient, listingId)).Items.Should().BeEmpty();
        ReviewResponse own = (await GetReviewsAsync(guest, listingId)).Items.Should().ContainSingle().Subject;
        own.Should().BeEquivalentTo(new { Id = reviewId, IsPublished = false, Rating = 4, Categories, HostReply = (HostReplyResponse?)null });
        (await HttpClient.GetFromJsonAsync<ApartmentResponse>($"api/v1/apartments/{listingId}"))!
            .Should().BeEquivalentTo(new { ReviewCount = 0, AverageRating = (decimal?)null, RatingBreakdown = (RatingBreakdownResponse?)null });
        (await GetBookingAsync(guest, bookingId)).Should().BeEquivalentTo(new { CanReview = false, HasReview = true });
        (await host.GetFromJsonAsync<PagedList<ReviewResponse>>("api/v1/hosting/reviews"))!.Items.Should().BeEmpty();

        // Unpublished reviews can still be edited.
        (await guest.PutAsJsonAsync(
                $"api/v1/reviews/{reviewId}",
                new UpdateReviewRequest(guestId, 5, "Even better than expected.", Categories)))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        // The host reviews the guest: both reviews are published.
        HttpResponseMessage guestReview = await host.PostAsJsonAsync(
            $"api/v1/hosting/bookings/{bookingId}/guest-review",
            new GuestReviewRequest(5, "A wonderful guest, welcome back anytime."));
        guestReview.StatusCode.Should().Be(HttpStatusCode.Created, await guestReview.Content.ReadAsStringAsync());

        await ShouldFailWithAsync(
            await host.PostAsJsonAsync($"api/v1/hosting/bookings/{bookingId}/guest-review", new GuestReviewRequest(4, "Again")),
            HttpStatusCode.BadRequest,
            "Review.AlreadyReviewed");

        ReviewResponse published = (await GetReviewsAsync(HttpClient, listingId)).Items.Should().ContainSingle().Subject;
        published.Should().BeEquivalentTo(new
        {
            Id = reviewId,
            IsPublished = true,
            Rating = 5,
            Comment = "Even better than expected.",
            AuthorName = "First L.",
            AuthorAvatarUrl = (string?)null,
            BookingId = bookingId,
        });

        ApartmentResponse listing = (await HttpClient.GetFromJsonAsync<ApartmentResponse>($"api/v1/apartments/{listingId}"))!;
        listing.ReviewCount.Should().Be(1);
        listing.AverageRating.Should().Be(5.0m);
        listing.RatingBreakdown.Should().Be(new RatingBreakdownResponse(5, 4, 5, 5, 3, 4));

        // Published reviews are final.
        await ShouldFailWithAsync(
            await guest.PutAsJsonAsync($"api/v1/reviews/{reviewId}", new UpdateReviewRequest(guestId, 1, "Changed my mind", Categories)),
            HttpStatusCode.Conflict,
            "Review.AlreadyPublished");

        UserProfileResponse? profile = await HttpClient.GetFromJsonAsync<UserProfileResponse>($"api/v1/users/{guestId}/profile");
        profile!.GuestReviewCount.Should().Be(1);
        profile.GuestAverageRating.Should().Be(5.0m);
        profile.GuestReviews.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Rating = 5,
            Comment = "A wonderful guest, welcome back anytime.",
            HostFirstName = "First",
            HostAvatarUrl = (string?)null,
        });
        profile.GuestReviews[0].ListingName.Should().StartWith("Zz Test listing");

        BookingResponse forHost = await GetBookingAsync(host, bookingId);
        forHost.Should().BeEquivalentTo(new { CanReview = false, HasGuestReview = true, HasReview = true });
    }

    [Fact]
    public async Task Reviews_Should_RequireValidCategories_AndAnOpenWindow()
    {
        (HttpClient host, _, Guid listingId) = await CreateHostWithListingAsync();
        (HttpClient guest, Guid guestId) = await CreateAuthenticatedUserAsync();
        Guid bookingId = await CreateCompletedStayAsync(listingId, guestId, checkedOutDaysAgo: 1);

        await ShouldFailWithAsync(
            await PostReviewRequestAsync(guest, new CreateReviewRequest(guestId, bookingId, 5, "Great", null)),
            HttpStatusCode.BadRequest,
            "Review.InvalidCategoryRating");
        await ShouldFailWithAsync(
            await PostReviewRequestAsync(guest, new CreateReviewRequest(guestId, bookingId, 5, "Great", Categories with { Value = 6 })),
            HttpStatusCode.BadRequest,
            "Review.InvalidCategoryRating");
        await ShouldFailWithAsync(
            await PostReviewRequestAsync(guest, new CreateReviewRequest(guestId, bookingId, 5, "Great", Categories with { CheckIn = 0 })),
            HttpStatusCode.BadRequest,
            "Review.InvalidCategoryRating");

        // Checked out 15 days ago: the window (14 days) has closed for both sides.
        Guid oldBookingId = await CreateCompletedStayAsync(listingId, guestId, checkedOutDaysAgo: 15, nights: 1);
        (await GetBookingAsync(guest, oldBookingId)).CanReview.Should().BeFalse();
        await ShouldFailWithAsync(
            await PostReviewRequestAsync(guest, new CreateReviewRequest(guestId, oldBookingId, 5, "Late", Categories)),
            HttpStatusCode.BadRequest,
            "Booking.ReviewWindowClosed");
        await ShouldFailWithAsync(
            await host.PostAsJsonAsync($"api/v1/hosting/bookings/{oldBookingId}/guest-review", new GuestReviewRequest(5, "Late")),
            HttpStatusCode.BadRequest,
            "Booking.ReviewWindowClosed");

        // Upcoming stays cannot be reviewed.
        (HttpClient otherGuest, Guid otherGuestId, Guid upcomingId) = await BookAsGuestAsync(listingId, NextFreeDate(), 2);
        await ShouldFailWithAsync(
            await PostReviewRequestAsync(otherGuest, new CreateReviewRequest(otherGuestId, upcomingId, 5, "Early", Categories)),
            HttpStatusCode.Forbidden,
            "Review.NotEligible");
        await ShouldFailWithAsync(
            await host.PostAsJsonAsync($"api/v1/hosting/bookings/{upcomingId}/guest-review", new GuestReviewRequest(0, "")),
            HttpStatusCode.BadRequest,
            "CreateReview.RatingMustBeBetweenOneAndFive");
    }

    [Fact]
    public async Task ReviewWindowJob_Should_PublishReviewsWhenTheWindowCloses()
    {
        (_, _, Guid listingId) = await CreateHostWithListingAsync();
        (HttpClient guest, Guid guestId) = await CreateAuthenticatedUserAsync();
        Guid bookingId = await CreateCompletedStayAsync(listingId, guestId, checkedOutDaysAgo: 3);
        Guid reviewId = await PostReviewAsync(guest, guestId, bookingId, 3, Categories);

        await RunAsync<IPublishReviewsJob, int>(job => job.PublishExpiredAsync());
        (await GetReviewsAsync(HttpClient, listingId)).Items.Should().BeEmpty();

        // Time passes: the stay checked out 15 days ago, and the host never reviewed the guest.
        await ExecuteSqlAsync(
            $"UPDATE bookings SET duration_start = duration_start - 12, duration_end = duration_end - 12 WHERE id = {bookingId}");

        (await RunAsync<IPublishReviewsJob, int>(job => job.PublishExpiredAsync())).Should().BeGreaterThanOrEqualTo(1);

        (await GetReviewsAsync(HttpClient, listingId)).Items.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Id = reviewId, IsPublished = true });
        (await HttpClient.GetFromJsonAsync<ApartmentResponse>($"api/v1/apartments/{listingId}"))!.ReviewCount.Should().Be(1);
    }

    [Fact]
    public async Task Hosts_Should_ReplyToPublishedReviewsOfTheirListings()
    {
        (HttpClient host, _, Guid listingId) = await CreateHostWithListingAsync();
        (HttpClient guest, Guid guestId) = await CreateAuthenticatedUserAsync();
        Guid bookingId = await CreateCompletedStayAsync(listingId, guestId, checkedOutDaysAgo: 1);
        Guid reviewId = await PostReviewAsync(guest, guestId, bookingId, 4, Categories);

        await ShouldFailWithAsync(
            await host.PutAsJsonAsync($"api/v1/hosting/reviews/{reviewId}/reply", new ReviewReplyRequest("Thanks!")),
            HttpStatusCode.BadRequest,
            "Review.NotPublished");

        (await host.PostAsJsonAsync($"api/v1/hosting/bookings/{bookingId}/guest-review", new GuestReviewRequest(4, "Nice guest")))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        // Listed for the host, unreplied.
        PagedList<ReviewResponse>? hostReviews = await host.GetFromJsonAsync<PagedList<ReviewResponse>>(
            $"api/v1/hosting/reviews?listingId={listingId}");
        hostReviews!.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Id = reviewId, HostReply = (HostReplyResponse?)null });
        hostReviews.Items[0].ListingName.Should().StartWith("Zz Test listing");

        await ShouldFailWithAsync(
            await host.PutAsJsonAsync($"api/v1/hosting/reviews/{reviewId}/reply", new ReviewReplyRequest(new string('x', 1001))),
            HttpStatusCode.BadRequest,
            "Review.ReplyTooLong");

        (HttpClient otherHost, _) = await CreateAuthenticatedUserAsync();
        await ShouldFailWithAsync(
            await otherHost.PutAsJsonAsync($"api/v1/hosting/reviews/{reviewId}/reply", new ReviewReplyRequest("Hi")),
            HttpStatusCode.Forbidden,
            "Apartment.NotOwner");

        (await host.PutAsJsonAsync($"api/v1/hosting/reviews/{reviewId}/reply", new ReviewReplyRequest("Thanks for staying!")))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        HostReplyResponse reply = (await GetReviewsAsync(HttpClient, listingId)).Items.Single().HostReply!;
        reply.Text.Should().Be("Thanks for staying!");
        reply.ModifiedOnUtc.Should().BeNull();

        (await host.PutAsJsonAsync($"api/v1/hosting/reviews/{reviewId}/reply", new ReviewReplyRequest("Thanks again!")))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        HostReplyResponse edited = (await GetReviewsAsync(HttpClient, listingId)).Items.Single().HostReply!;
        edited.Should().BeEquivalentTo(new { Text = "Thanks again!", reply.CreatedOnUtc });
        edited.ModifiedOnUtc.Should().NotBeNull();

        (await host.DeleteAsync($"api/v1/hosting/reviews/{reviewId}/reply")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await GetReviewsAsync(HttpClient, listingId)).Items.Single().HostReply.Should().BeNull();
    }

    [Fact]
    public async Task SeededReviews_Should_HaveCategories_Replies_AndGuestReviews()
    {
        ApartmentResponse cabin = await FindApartmentAsync("Pine Ridge Cabin");

        cabin.RatingBreakdown.Should().NotBeNull();
        cabin.CancellationPolicy.Should().Be("Moderate");

        PagedList<ReviewResponse>? reviews = await HttpClient.GetFromJsonAsync<PagedList<ReviewResponse>>(
            $"api/v1/apartments/{cabin.Id}/reviews");
        reviews!.Items.Should().OnlyContain(r => r.IsPublished && r.Categories.Cleanliness >= 1 && r.Categories.Value >= 1);
        reviews.Items.Should().ContainSingle(r => r.HostReply != null);

        (HttpClient sam, _) = await LoginAsHostAsync();
        PagedList<ReviewResponse>? samReviews = await sam.GetFromJsonAsync<PagedList<ReviewResponse>>(
            "api/v1/hosting/reviews?pageSize=50");
        samReviews!.Items.Count(r => r.HostReply != null).Should().BeGreaterThanOrEqualTo(2);

        (_, Guid jamieId) = await LoginAsync("jamie.lee@lodge.dev", DemoPassword);
        UserProfileResponse? jamie = await HttpClient.GetFromJsonAsync<UserProfileResponse>($"api/v1/users/{jamieId}/profile");
        jamie!.GuestReviewCount.Should().Be(1);
        jamie.GuestReviews.Should().ContainSingle().Which.HostFirstName.Should().Be("Sam");
    }

    [Fact]
    public async Task Review_Should_ReplayTheFirstResult_ForTheSameIdempotencyKey()
    {
        (_, _, Guid listingId) = await CreateHostWithListingAsync();
        (HttpClient guest, Guid guestId) = await CreateAuthenticatedUserAsync();
        Guid bookingId = await CreateCompletedStayAsync(listingId, guestId, checkedOutDaysAgo: 2);
        var review = new CreateReviewRequest(guestId, bookingId, 5, "A lovely stay.", Categories);
        string key = Guid.NewGuid().ToString();

        HttpResponseMessage first = await PostReviewRequestAsync(guest, review, key);
        HttpResponseMessage replay = await PostReviewRequestAsync(guest, review, key);

        first.StatusCode.Should().Be(HttpStatusCode.OK, await first.Content.ReadAsStringAsync());
        replay.StatusCode.Should().Be(HttpStatusCode.OK, await replay.Content.ReadAsStringAsync());
        (await replay.Content.ReadFromJsonAsync<Guid>()).Should().Be(await first.Content.ReadFromJsonAsync<Guid>());

        // A new key is a new request: the booking already has its review.
        await ShouldFailWithAsync(await PostReviewRequestAsync(guest, review), HttpStatusCode.BadRequest, "Review.AlreadyReviewed");
    }

    private static async Task<PagedList<ReviewResponse>> GetReviewsAsync(HttpClient client, Guid listingId) =>
        (await client.GetFromJsonAsync<PagedList<ReviewResponse>>($"api/v1/apartments/{listingId}/reviews"))!;

    private static async Task<Guid> PostReviewAsync(
        HttpClient guest,
        Guid guestId,
        Guid bookingId,
        int rating,
        CategoryRatingsDto categories)
    {
        HttpResponseMessage response = await PostReviewRequestAsync(
            guest,
            new CreateReviewRequest(guestId, bookingId, rating, "A lovely stay.", categories));
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private static async Task<HttpResponseMessage> PostReviewRequestAsync(
        HttpClient client,
        CreateReviewRequest review,
        string? idempotencyKey = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/reviews") { Content = JsonContent.Create(review) };
        request.Headers.Add("X-Idempotency-Key", idempotencyKey ?? Guid.NewGuid().ToString());

        return await client.SendAsync(request);
    }
}
