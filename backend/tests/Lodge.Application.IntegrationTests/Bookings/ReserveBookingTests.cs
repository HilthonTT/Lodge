using Lodge.Application.Abstractions.Idempotency;
using Lodge.Application.Apartments.Search;
using Lodge.Application.Bookings.Cancel;
using Lodge.Application.Bookings.Reserve;
using Lodge.Application.Core.Errors;
using Lodge.Application.Files.Upload;
using Lodge.Application.IntegrationTests.Abstractions;
using Lodge.Contracts.Apartments;
using Lodge.Contracts.Common;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;
using Lodge.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Lodge.Application.IntegrationTests.Bookings;

public class ReserveBookingTests(IntegrationTestWebFactory factory) : BaseIntegrationTest(factory)
{
    // Far in the future and unique per test so seeded and parallel bookings never interfere.
    private static int _offset = 2000;

    [Fact]
    public async Task Reserve_Should_ReplayTheFirstResult_ForARepeatedIdempotencyKey()
    {
        Guid userId = await CreateAndSignInUserAsync();
        Guid apartmentId = await GetApartmentIdAsync();
        (DateOnly start, DateOnly end) = NextPeriod();
        var command = new ReserveBookingCommand(Guid.NewGuid(), apartmentId, userId, start, end, GuestCount: 1);

        Result<Guid> first = await Sender.Send(command);
        Result<Guid> replay = await Sender.Send(command);

        first.IsSuccess.Should().BeTrue();
        replay.IsSuccess.Should().BeTrue();
        replay.Value.Should().Be(first.Value);
        (await DbContext.Bookings.CountAsync(b => b.UserId == userId)).Should().Be(1, "the replay must not book again");
    }

    [Fact]
    public async Task IdempotencyKey_Should_BeRejected_WhenReusedForAnotherRequestType()
    {
        Guid userId = await CreateAndSignInUserAsync();
        Guid apartmentId = await GetApartmentIdAsync();
        (DateOnly start, DateOnly end) = NextPeriod();
        Guid requestId = Guid.NewGuid();

        (await Sender.Send(new ReserveBookingCommand(requestId, apartmentId, userId, start, end, GuestCount: 1)))
            .IsSuccess.Should().BeTrue();

        using var stream = new MemoryStream([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        Result<Guid> upload = await Sender.Send(new UploadFileCommand(requestId, userId, stream, stream.Length, "image/png"));

        upload.Error.Should().Be(IdempotencyErrors.KeyReused);
    }

    [Fact]
    public async Task IdempotencyKey_Should_BeInProgress_WhileAnotherRequestWithTheKeyIsRunning()
    {
        Guid userId = await CreateAndSignInUserAsync();
        Guid apartmentId = await GetApartmentIdAsync();
        (DateOnly start, DateOnly end) = NextPeriod();
        var command = new ReserveBookingCommand(Guid.NewGuid(), apartmentId, userId, start, end, GuestCount: 1);

        // Another request holds the key in its (uncommitted) transaction.
        using (IServiceScope other = Factory.Services.CreateScope())
        {
            LodgeDbContext otherContext = other.ServiceProvider.GetRequiredService<LodgeDbContext>();
            await using IDbContextTransaction transaction = await otherContext.Database.BeginTransactionAsync();

            (await other.ServiceProvider.GetRequiredService<IIdempotencyService>()
                    .BeginAsync(command.RequestId, nameof(ReserveBookingCommand)))
                .Status.Should().Be(IdempotencyStatus.New);

            (await Sender.Send(command)).Error.Should().Be(IdempotencyErrors.InProgress);

            await transaction.RollbackAsync();
        }

        // The other request rolled back without using the key, so it is free again.
        (await Sender.Send(command)).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Reserve_Should_NotConsumeIdempotencyKey_WhenCommandFails()
    {
        Guid userId = await CreateAndSignInUserAsync();
        Guid apartmentId = await GetApartmentIdAsync();
        (DateOnly start, DateOnly end) = NextPeriod();
        Guid requestId = Guid.NewGuid();

        Result<Guid> failed = await Sender.Send(new ReserveBookingCommand(requestId, Guid.NewGuid(), userId, start, end, GuestCount: 1));
        Result<Guid> retried = await Sender.Send(new ReserveBookingCommand(requestId, apartmentId, userId, start, end, GuestCount: 1));

        failed.IsFailure.Should().BeTrue();
        retried.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Reserve_Should_Fail_WhenPeriodOverlapsExistingBooking()
    {
        Guid userId = await CreateAndSignInUserAsync();
        Guid apartmentId = await GetApartmentIdAsync();
        (DateOnly start, DateOnly end) = NextPeriod();

        await Sender.Send(new ReserveBookingCommand(Guid.NewGuid(), apartmentId, userId, start, end, GuestCount: 1));
        Result<Guid> overlapping = await Sender.Send(
            new ReserveBookingCommand(Guid.NewGuid(), apartmentId, userId, start.AddDays(1), end.AddDays(1), GuestCount: 1));

        overlapping.Error.Should().Be(BookingErrors.Overlap);
    }

    [Fact]
    public async Task Reserve_Should_Succeed_WhenStayStartsOnAnotherStaysCheckOutDay()
    {
        Guid userId = await CreateAndSignInUserAsync();
        Guid apartmentId = await GetApartmentIdAsync();
        (DateOnly start, DateOnly end) = NextPeriod();

        await Sender.Send(new ReserveBookingCommand(Guid.NewGuid(), apartmentId, userId, start, end, GuestCount: 1));
        Result<Guid> backToBack = await Sender.Send(
            new ReserveBookingCommand(Guid.NewGuid(), apartmentId, userId, end, end.AddDays(2), GuestCount: 1));

        backToBack.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Cancel_Should_Fail_ForAnotherUsersBooking()
    {
        Guid ownerId = await CreateAndSignInUserAsync();
        Guid apartmentId = await GetApartmentIdAsync();
        (DateOnly start, DateOnly end) = NextPeriod();
        Result<Guid> booking = await Sender.Send(new ReserveBookingCommand(Guid.NewGuid(), apartmentId, ownerId, start, end, GuestCount: 1));

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
        await Sender.Send(new ReserveBookingCommand(Guid.NewGuid(), apartmentId, userId, start, end, GuestCount: 1));

        Result<PagedList<ApartmentResponse>> result = await Sender.Send(
            new SearchApartmentQuery { Page = 1, PageSize = 100, StartDate = start, EndDate = end });

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().NotContain(a => a.Id == apartmentId);
        result.Value.Items.Should().NotBeEmpty();
    }

    private async Task<Guid> GetApartmentIdAsync() =>
        await DbContext.Apartments
            .Where(a => a.InstantBook && a.IsListed && a.MinNights == 1)
            .OrderBy(a => a.Id)
            .Select(a => a.Id)
            .FirstAsync();

    private static (DateOnly Start, DateOnly End) NextPeriod()
    {
        int offset = Interlocked.Add(ref _offset, 10);
        DateOnly start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(offset);

        return (start, start.AddDays(3));
    }
}
