using Lodge.Domain.Users;
using Lodge.Domain.Wishlists;

namespace Lodge.Domain.UnitTests.Users;

public class HostingTests
{
    private static readonly DateTime UtcNow = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(5, "4.8", 0, 10, true)]
    [InlineData(4, "5.0", 0, 10, false)]
    [InlineData(10, "4.79", 0, 10, false)]
    [InlineData(10, "5.0", 1, 10, false)]
    [InlineData(10, "5.0", 1, 11, true)]
    [InlineData(0, "5.0", 0, 0, false)]
    public void HostPerformance_Should_MeetTheDefaultRequirements_OnlyWhenEveryRuleHolds(
        int completedStays,
        string averageRating,
        int hostCancellations,
        int totalBookings,
        bool expected)
    {
        var performance = new HostPerformance(completedStays, decimal.Parse(averageRating, System.Globalization.CultureInfo.InvariantCulture), 3, hostCancellations, totalBookings);

        performance.Meets(SuperhostRequirements.Default).Should().Be(expected);
    }

    [Fact]
    public void HostPerformance_Should_NotMeetTheRequirements_WithoutReviews()
    {
        new HostPerformance(20, null, 0, 0, 20).Meets(SuperhostRequirements.Default).Should().BeFalse();
        new HostPerformance(20, null, 0, 0, 0).CancellationRate.Should().Be(0);
    }

    [Fact]
    public void Wishlist_Should_ValidateItsName()
    {
        Wishlist.Create(Guid.NewGuid(), "   ", UtcNow).Error.Should().Be(WishlistErrors.NameRequired);
        Wishlist.Create(Guid.NewGuid(), new string('x', 51), UtcNow).Error.Should().Be(WishlistErrors.NameTooLong);
        Wishlist.Create(Guid.NewGuid(), "  Summer  ", UtcNow).Value.Name.Should().Be("Summer");
    }

    [Fact]
    public void Wishlist_Should_AddAndRemoveItems_Idempotently()
    {
        Wishlist wishlist = Wishlist.Create(Guid.NewGuid(), "Summer", UtcNow).Value;
        Guid apartmentId = Guid.NewGuid();

        wishlist.AddItem(apartmentId, UtcNow.AddHours(1));
        wishlist.AddItem(apartmentId, UtcNow.AddHours(2));

        wishlist.Items.Should().ContainSingle().Which.AddedOnUtc.Should().Be(UtcNow.AddHours(1));
        wishlist.UpdatedOnUtc.Should().Be(UtcNow.AddHours(1));

        wishlist.RemoveItem(apartmentId, UtcNow.AddHours(3));
        wishlist.RemoveItem(apartmentId, UtcNow.AddHours(4));

        wishlist.Items.Should().BeEmpty();
        wishlist.UpdatedOnUtc.Should().Be(UtcNow.AddHours(3));
    }

    [Fact]
    public void User_Should_BecomeAndStopBeingASuperhost()
    {
        var user = User.Create(
            FirstName.Create("Sam").Value,
            LastName.Create("Rivera").Value,
            Email.Create("sam@lodge.test").Value,
            "hash");

        user.IsSuperhost.Should().BeFalse();
        user.SetSuperhost(true);
        user.IsSuperhost.Should().BeTrue();
        user.SetSuperhost(false);
        user.IsSuperhost.Should().BeFalse();
    }
}
