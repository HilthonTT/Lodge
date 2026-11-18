using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Reviews;

namespace Lodge.Domain.UnitTests.Reviews;

public class ReviewTests
{
    private static readonly DateTime UtcNow = new(2026, 6, 20, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = DateOnly.FromDateTime(UtcNow);
    private static readonly Rating Five = Rating.Create(5).Value;
    private static readonly Comment Lovely = Comment.Create("Lovely").Value;
    private static readonly CategoryRatings Categories = CategoryRatings.Create(5, 4, 5, 5, 3, 4).Value;

    [Theory]
    [InlineData(0, 5, 5, 5, 5, 5)]
    [InlineData(5, 6, 5, 5, 5, 5)]
    [InlineData(5, 5, 5, 5, 5, -1)]
    public void CategoryRatings_Should_RequireOneToFive(int c, int a, int ci, int co, int l, int v)
    {
        CategoryRatings.Create(c, a, ci, co, l, v).Error.Should().Be(ReviewErrors.InvalidCategoryRating);
    }

    [Fact]
    public void Create_Should_BeUnpublished_WithinTheReviewWindow()
    {
        // Checked out 14 days ago: the last day of the window.
        Booking booking = CompletedStay(checkOut: Today.AddDays(-14));

        Review review = Review.Create(booking, Five, Categories, Lovely, UtcNow).Value;

        review.IsPublished.Should().BeFalse();
        review.Categories.Should().Be(Categories);
        booking.ReviewDeadline.Should().Be(Today);
    }

    [Fact]
    public void Create_Should_Fail_AfterTheReviewWindow_OrBeforeCompletion()
    {
        Review.Create(CompletedStay(checkOut: Today.AddDays(-15)), Five, Categories, Lovely, UtcNow)
            .Error.Should().Be(BookingErrors.ReviewWindowClosed);

        Apartment apartment = TestData.CreateApartment(100, 0);
        Booking confirmed = TestData.Reserve(apartment, Today.AddDays(-3), 2, UtcNow.AddDays(-10));
        confirmed.Confirm(UtcNow.AddDays(-10));

        Review.Create(confirmed, Five, Categories, Lovely, UtcNow).Error.Should().Be(ReviewErrors.NotEligible);
        GuestReview.Create(confirmed, TestData.OwnerId, Five, Lovely, UtcNow).Error.Should().Be(ReviewErrors.NotEligible);
    }

    [Fact]
    public void PublishedReviews_Should_BeFinal_AndAcceptHostReplies()
    {
        Review review = Review.Create(CompletedStay(Today.AddDays(-2)), Five, Categories, Lovely, UtcNow).Value;

        review.Reply("Thanks!", UtcNow).Error.Should().Be(ReviewErrors.NotPublished);
        review.Update(Rating.Create(4).Value, Categories, Lovely).IsSuccess.Should().BeTrue();

        review.Publish(UtcNow);

        review.Update(Five, Categories, Lovely).Error.Should().Be(ReviewErrors.AlreadyPublished);
        review.Reply("  ", UtcNow).Error.Should().Be(ReviewErrors.ReplyEmpty);
        review.Reply(new string('x', Review.ReplyMaxLength + 1), UtcNow).Error.Should().Be(ReviewErrors.ReplyTooLong);

        review.Reply(" Thanks! ", UtcNow).IsSuccess.Should().BeTrue();
        review.HostReply.Should().Be("Thanks!");
        review.HostReplyCreatedOnUtc.Should().Be(UtcNow);
        review.HostReplyModifiedOnUtc.Should().BeNull();

        review.Reply("Thank you!", UtcNow.AddHours(1)).IsSuccess.Should().BeTrue();
        review.HostReplyModifiedOnUtc.Should().Be(UtcNow.AddHours(1));

        review.RemoveReply();
        review.HostReply.Should().BeNull();
    }

    [Fact]
    public void GuestReview_Should_TargetTheGuestOfTheStay()
    {
        Booking booking = CompletedStay(Today.AddDays(-1));
        Guid hostId = Guid.NewGuid();

        GuestReview review = GuestReview.Create(booking, hostId, Five, Lovely, UtcNow).Value;

        review.GuestId.Should().Be(booking.UserId);
        review.HostId.Should().Be(hostId);
        review.BookingId.Should().Be(booking.Id);
        review.IsPublished.Should().BeFalse();
    }

    private static Booking CompletedStay(DateOnly checkOut)
    {
        Apartment apartment = TestData.CreateApartment(100, 0);
        DateTime bookedOn = checkOut.AddDays(-30).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        Booking booking = TestData.Reserve(apartment, checkOut.AddDays(-2), 2, bookedOn);
        booking.Confirm(bookedOn);
        booking.Complete(checkOut.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc));

        return booking;
    }
}
