using Lodge.Application.Apartments.Search;
using Lodge.Application.Bookings.Cancel;
using Lodge.Application.Bookings.Reserve;
using Lodge.Application.Core.Errors;
using Lodge.Application.IntegrationTests.Abstractions;
using Lodge.Contracts.Apartments;
using Lodge.Contracts.Common;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Application.IntegrationTests.Bookings;

public class ReserveBookingTests(IntegrationTestWebFactory factory) : BaseIntegrationTest(factory)
{
    // Far in the future and unique per test so seeded and parallel bookings never interfere.
    private static int _offset = 2000;

    [Fact]
    public async Task Reserve_Should_RejectReplayedIdempotencyKey()
    {
        Guid userId = await CreateAndSignInUserAsync();
        Guid apartmentId = await GetApartmentIdAsync();
        (DateOnly start, DateOnly end) = NextPeriod();
        var command = new ReserveBookingCommand(Guid.NewGuid(), apartmentId, userId, start, end);

        Result<Guid> first = await Sender.Send(command);
        Result<Guid> replay = await Sender.Send(command);

        first.IsSuccess.Should().BeTrue();
        replay.Error.Should().Be(IdempotencyErrors.AlreadyProcessed);
    }

    [Fact]
    public async Task Reserve_Should_NotConsumeIdempotencyKey_WhenCommandFails()
    {
        Guid userId = await CreateAndSignInUserAsync();
        Guid apartmentId = await GetApartmentIdAsync();
        (DateOnly start, DateOnly end) = NextPeriod();
        Guid requestId = Guid.NewGuid();

        Result<Guid> failed = await Sender.Send(new ReserveBookingCommand(requestId, Guid.NewGuid(), userId, start, end));
        Result<Guid> retried = await Sender.Send(new ReserveBookingCommand(requestId, apartmentId, userId, start, end));

        failed.IsFailure.Should().BeTrue();
        retried.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Reserve_Should_Fail_WhenPeriodOverlapsExistingBooking()
    {
        Guid userId = await CreateAndSignInUserAsync();
        Guid apartmentId = await GetApartmentIdAsync();
        (DateOnly start, DateOnly end) = NextPeriod();

        await Sender.Send(new ReserveBookingCommand(Guid.NewGuid(), apartmentId, userId, start, end));
        Result<Guid> overlapping = await Sender.Send(
            new ReserveBookingCommand(Guid.NewGuid(), apartmentId, userId, start.AddDays(1), end.AddDays(1)));

        overlapping.Error.Should().Be(BookingErrors.Overlap);
    }

    [Fact]
    public async Task Reserve_Should_Succeed_WhenStayStartsOnAnotherStaysCheckOutDay()
    {
        Guid userId = await CreateAndSignInUserAsync();
        Guid apartmentId = await GetApartmentIdAsync();
        (DateOnly start, DateOnly end) = NextPeriod();

        await Sender.Send(new ReserveBookingCommand(Guid.NewGuid(), apartmentId, userId, start, end));
        Result<Guid> backToBack = await Sender.Send(
            new ReserveBookingCommand(Guid.NewGuid(), apartmentId, userId, end, end.AddDays(2)));

        backToBack.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Cancel_Should_Fail_ForAnotherUsersBooking()
    {
        Guid ownerId = await CreateAndSignInUserAsync();
        Guid apartmentId = await GetApartmentIdAsync();
        (DateOnly start, DateOnly end) = NextPeriod();
        Result<Guid> booking = await Sender.Send(new ReserveBookingCommand(Guid.NewGuid(), apartmentId, ownerId, start, end));

        Guid otherUserId = await CreateAndSignInUserAsync();
        Result result = await Sender.Send(new CancelBookingCommand(otherUserId, booking.Value));

        result.Error.Should().Be(UserErrors.InvalidPermissions);
        (await DbContext.Bookings.AsNoTracking().SingleAsync(b => b.Id == booking.Value)).Status
            .Should().Be(BookingStatus.Reserved);
    }

    [Fact]
    public async Task Search_Should_ExcludeApartmentsBookedForTheRequestedPeriod()
    {
        Guid userId = await CreateAndSignInUserAsync();
        Guid apartmentId = await GetApartmentIdAsync();
        (DateOnly start, DateOnly end) = NextPeriod();
        await Sender.Send(new ReserveBookingCommand(Guid.NewGuid(), apartmentId, userId, start, end));

        Result<PagedList<ApartmentResponse>> result = await Sender.Send(
            new SearchApartmentQuery(null, null, null, 1, 100, start, end, null, null));

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().NotContain(a => a.Id == apartmentId);
        result.Value.Items.Should().NotBeEmpty();
    }

    private async Task<Guid> GetApartmentIdAsync() =>
        await DbContext.Apartments.OrderBy(a => a.Id).Select(a => a.Id).FirstAsync();

    private static (DateOnly Start, DateOnly End) NextPeriod()
    {
        int offset = Interlocked.Add(ref _offset, 10);
        DateOnly start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(offset);

        return (start, start.AddDays(3));
    }
}
