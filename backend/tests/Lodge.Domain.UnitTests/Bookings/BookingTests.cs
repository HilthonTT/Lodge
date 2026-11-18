using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Bookings.Events;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Shared;

namespace Lodge.Domain.UnitTests.Bookings;

public class BookingTests
{
    private static readonly DateTime UtcNow = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = DateOnly.FromDateTime(UtcNow);

    [Fact]
    public void Reserve_Should_SetStatusToReserved_And_RaiseDomainEvent()
    {
        Booking booking = CreateBooking(Today.AddDays(10), nights: 3);

        booking.Status.Should().Be(BookingStatus.Reserved);
        booking.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<BookingReservedDomainEvent>();
    }

    [Fact]
    public void Confirm_Should_SetConfirmedOnUtc()
    {
        Booking booking = CreateBooking(Today.AddDays(10), nights: 3);

        Result result = booking.Confirm(UtcNow);

        result.IsSuccess.Should().BeTrue();
        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.ConfirmedOnUtc.Should().Be(UtcNow);
    }

    [Fact]
    public void Reject_Should_SetRejectedOnUtc_NotConfirmedOnUtc()
    {
        Booking booking = CreateBooking(Today.AddDays(10), nights: 3);

        booking.Reject(UtcNow).IsSuccess.Should().BeTrue();

        booking.Status.Should().Be(BookingStatus.Rejected);
        booking.RejectedOnUtc.Should().Be(UtcNow);
        booking.ConfirmedOnUtc.Should().BeNull();
    }

    [Fact]
    public void Complete_Should_Fail_WhenStayHasNotEnded()
    {
        Booking booking = CreateBooking(Today.AddDays(-1), nights: 3);
        booking.Confirm(UtcNow);

        Result result = booking.Complete(UtcNow);

        result.Error.Should().Be(BookingErrors.NotEnded);
        booking.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public void Complete_Should_SetCompletedOnUtc_WhenStayHasEnded()
    {
        Booking booking = CreateBooking(Today.AddDays(-5), nights: 3);
        booking.Confirm(UtcNow.AddDays(-10));

        booking.Complete(UtcNow).IsSuccess.Should().BeTrue();

        booking.CompletedOnUtc.Should().Be(UtcNow);
        booking.ConfirmedOnUtc.Should().Be(UtcNow.AddDays(-10));
    }

    [Fact]
    public void Cancel_Should_BeAllowed_ForReservedBooking()
    {
        Booking booking = CreateBooking(Today.AddDays(10), nights: 3);

        booking.Cancel(UtcNow).IsSuccess.Should().BeTrue();

        booking.Status.Should().Be(BookingStatus.Cancelled);
        booking.CancelledOnUtc.Should().Be(UtcNow);
        booking.DomainEvents.Should().ContainItemsAssignableTo<BookingCancelledDomainEvent>();
    }

    [Fact]
    public void Cancel_Should_Fail_WhenStayAlreadyStarted()
    {
        Booking booking = CreateBooking(Today.AddDays(-1), nights: 3);
        booking.Confirm(UtcNow);

        booking.Cancel(UtcNow).Error.Should().Be(BookingErrors.AlreadyStarted);
    }

    [Fact]
    public void Cancel_Should_Fail_ForRejectedBooking()
    {
        Booking booking = CreateBooking(Today.AddDays(10), nights: 3);
        booking.Reject(UtcNow);

        booking.Cancel(UtcNow).Error.Should().Be(BookingErrors.NotCancellable);
    }

    private static Booking CreateBooking(DateOnly start, int nights)
    {
        Apartment apartment = TestData.CreateApartment(price: 100, cleaningFee: 0);
        DateRange duration = DateRange.Create(start, start.AddDays(nights)).Value;

        return Booking.Reserve(apartment, Guid.NewGuid(), duration, UtcNow, new PricingService());
    }
}

internal static class TestData
{
    public static Apartment CreateApartment(decimal price, decimal cleaningFee, params Amenity[] amenities)
    {
        Currency usd = Currency.FromCode("USD");

        return Apartment.Create(
            Name.Create("Test apartment").Value,
            Description.Create("A test apartment").Value,
            new Address("Country", "State", "1000", "City", "Street 1"),
            new Money(price, usd),
            new Money(cleaningFee, usd),
            maximumRoomCount: 2,
            maximumGuestCount: 4,
            imageUrl: "/images/cabin.png",
            amenities: [.. amenities]);
    }
}
