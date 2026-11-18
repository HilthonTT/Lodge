using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Bookings.Events;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.UnitTests.Bookings;

public class BookingRequestTests
{
    private static readonly DateTime UtcNow = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = DateOnly.FromDateTime(UtcNow);

    [Fact]
    public void Reserve_Should_CreateAPendingRequest_ForListingsWithoutInstantBook()
    {
        Booking booking = TestData.Reserve(RequestToBookApartment(), Today.AddDays(10), 3, UtcNow);

        booking.Status.Should().Be(BookingStatus.PendingApproval);
        booking.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<BookingRequestedDomainEvent>();
    }

    [Fact]
    public void Reserve_Should_RejectTooManyGuests_StaysThatAreTooShort_AndTheHostsOwnListing()
    {
        Apartment apartment = TestData.CreateApartment(TestData.Details() with { MinNights = 3 });
        DateRange twoNights = DateRange.Create(Today.AddDays(10), Today.AddDays(12)).Value;
        DateRange threeNights = DateRange.Create(Today.AddDays(10), Today.AddDays(13)).Value;
        var pricing = new PricingService();

        Booking.Reserve(apartment, Guid.NewGuid(), twoNights, 1, null, UtcNow, pricing.CalculatePrice(apartment, twoNights))
            .Error.Code.Should().Be("Booking.StayTooShort");

        Booking.Reserve(apartment, Guid.NewGuid(), threeNights, 5, null, UtcNow, pricing.CalculatePrice(apartment, threeNights))
            .Error.Code.Should().Be("Booking.InvalidGuestCount");

        Booking.Reserve(apartment, Guid.NewGuid(), threeNights, 0, null, UtcNow, pricing.CalculatePrice(apartment, threeNights))
            .Error.Code.Should().Be("Booking.InvalidGuestCount");

        Booking.Reserve(apartment, TestData.OwnerId, threeNights, 1, null, UtcNow, pricing.CalculatePrice(apartment, threeNights))
            .Error.Should().Be(BookingErrors.OwnListing);

        Result<Booking> booking = Booking.Reserve(
            apartment, Guid.NewGuid(), threeNights, 4, "  Hello!  ", UtcNow, pricing.CalculatePrice(apartment, threeNights));
        booking.Value.GuestCount.Should().Be(4);
        booking.Value.MessageToHost.Should().Be("Hello!");
    }

    [Fact]
    public void Accept_Should_TurnARequestIntoAReservation()
    {
        Booking booking = TestData.Reserve(RequestToBookApartment(), Today.AddDays(10), 3, UtcNow);
        booking.ClearDomainEvents();

        booking.Accept(UtcNow).IsSuccess.Should().BeTrue();

        booking.Status.Should().Be(BookingStatus.Reserved);
        booking.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<BookingReservedDomainEvent>();
        booking.Confirm(UtcNow).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Accept_Should_Fail_ForReservedBookings_AndRequestsThatAlreadyStarted()
    {
        TestData.Reserve(TestData.CreateApartment(100, 0), Today.AddDays(10), 3, UtcNow)
            .Accept(UtcNow).Error.Should().Be(BookingErrors.NotPendingApproval);

        TestData.Reserve(RequestToBookApartment(), Today.AddDays(-1), 3, UtcNow)
            .Accept(UtcNow).Error.Should().Be(BookingErrors.AlreadyStarted);
    }

    [Fact]
    public void Decline_Should_RejectARequest()
    {
        Booking booking = TestData.Reserve(RequestToBookApartment(), Today.AddDays(10), 3, UtcNow);

        booking.Decline(UtcNow).IsSuccess.Should().BeTrue();

        booking.Status.Should().Be(BookingStatus.Rejected);
        booking.RejectedOnUtc.Should().Be(UtcNow);
        booking.Decline(UtcNow).Error.Should().Be(BookingErrors.NotPendingApproval);
    }

    [Fact]
    public void Expire_Should_OnlyExpirePendingRequests()
    {
        Booking request = TestData.Reserve(RequestToBookApartment(), Today.AddDays(10), 3, UtcNow);
        Booking reservation = TestData.Reserve(TestData.CreateApartment(100, 0), Today.AddDays(10), 3, UtcNow);

        request.Expire(UtcNow).IsSuccess.Should().BeTrue();
        request.Status.Should().Be(BookingStatus.Expired);
        request.ExpiredOnUtc.Should().Be(UtcNow);
        request.DomainEvents.Should().ContainItemsAssignableTo<BookingExpiredDomainEvent>();

        reservation.Expire(UtcNow).Error.Should().Be(BookingErrors.NotPendingApproval);
    }

    [Fact]
    public void Cancel_Should_BeAllowed_ForPendingRequests()
    {
        Booking booking = TestData.Reserve(RequestToBookApartment(), Today.AddDays(10), 3, UtcNow);

        booking.Cancel(UtcNow).IsSuccess.Should().BeTrue();

        booking.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public void ChangeDates_Should_MoveTheStay_AndRecalculateThePrice()
    {
        Apartment apartment = TestData.CreateApartment(100, 0);
        Booking booking = TestData.Reserve(apartment, Today.AddDays(10), 3, UtcNow);
        DateRange newStay = DateRange.Create(Today.AddDays(20), Today.AddDays(25)).Value;

        Result result = booking.ChangeDates(apartment, newStay, UtcNow, new PricingService().CalculatePrice(apartment, newStay));

        result.IsSuccess.Should().BeTrue();
        booking.Duration.Should().Be(newStay);
        booking.TotalPrice.Amount.Should().Be(500);
        booking.Status.Should().Be(BookingStatus.Reserved);
    }

    [Fact]
    public void ChangeDates_Should_RequireACancellation_ForConfirmedBookings()
    {
        Apartment apartment = TestData.CreateApartment(100, 0);
        Booking booking = TestData.Reserve(apartment, Today.AddDays(10), 3, UtcNow);
        booking.Confirm(UtcNow);
        DateRange newStay = DateRange.Create(Today.AddDays(20), Today.AddDays(25)).Value;

        Result result = booking.ChangeDates(apartment, newStay, UtcNow, new PricingService().CalculatePrice(apartment, newStay));

        result.Error.Should().Be(BookingErrors.ChangeRequiresCancellation);
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public void ChangeDates_Should_EnforceTheStayLength()
    {
        Apartment apartment = TestData.CreateApartment(TestData.Details() with { MaxNights = 4 });
        Booking booking = TestData.Reserve(apartment, Today.AddDays(10), 3, UtcNow);
        DateRange newStay = DateRange.Create(Today.AddDays(20), Today.AddDays(25)).Value;

        booking.ChangeDates(apartment, newStay, UtcNow, new PricingService().CalculatePrice(apartment, newStay))
            .Error.Code.Should().Be("Booking.StayTooLong");
    }

    private static Apartment RequestToBookApartment() =>
        TestData.CreateApartment(TestData.Details() with { InstantBook = false });
}
