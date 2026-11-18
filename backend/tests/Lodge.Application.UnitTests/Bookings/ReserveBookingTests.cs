using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Bookings.Reserve;
using Lodge.Application.Core.Exceptions;
using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Shared;
using Lodge.Domain.Users;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;

namespace Lodge.Application.UnitTests.Bookings;

public class ReserveBookingTests
{
    private static readonly DateTime UtcNow = new(2026, 6, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = DateOnly.FromDateTime(UtcNow);

    private static readonly ReserveBookingCommand Command = new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Today.AddDays(7),
        Today.AddDays(10));

    private readonly Apartment _apartment = Apartment.Create(
        Name.Create("Name").Value,
        Description.Create("Description").Value,
        new Address("Country", "State", "ZipCode", "City", "Street"),
        new Money(100, Currency.FromCode("USD")),
        Money.Zero(Currency.FromCode("USD")),
        5,
        5,
        "/images/cabin.png",
        []);

    private readonly IUserIdentifierProvider _userIdentifierProvider = Substitute.For<IUserIdentifierProvider>();
    private readonly IApartmentRepository _apartmentRepository = Substitute.For<IApartmentRepository>();
    private readonly IBookingRepository _bookingRepository = Substitute.For<IBookingRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ReserveBookingCommandHandler _handler;

    public ReserveBookingTests()
    {
        _dateTimeProvider.UtcNow.Returns(UtcNow);
        _userIdentifierProvider.UserId.Returns(Command.UserId);
        _apartmentRepository.GetByIdAsync(Command.ApartmentId, Arg.Any<CancellationToken>()).Returns(_apartment);
        _bookingRepository
            .IsOverlappingAsync(Arg.Any<Apartment>(), Arg.Any<DateRange>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _handler = new ReserveBookingCommandHandler(
            _userIdentifierProvider,
            _apartmentRepository,
            _bookingRepository,
            _unitOfWork,
            new PricingService(),
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenUserLacksPermission()
    {
        _userIdentifierProvider.UserId.Returns(Guid.NewGuid());

        Result<Guid> result = await _handler.Handle(Command, default);

        result.Error.Should().Be(UserErrors.InvalidPermissions);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenApartmentIsNull()
    {
        _apartmentRepository.GetByIdAsync(Command.ApartmentId, Arg.Any<CancellationToken>()).ReturnsNull();

        Result<Guid> result = await _handler.Handle(Command, default);

        result.Error.Should().Be(ApartmentErrors.NotFound(Command.ApartmentId));
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenDateRangeIsInvalid()
    {
        ReserveBookingCommand invalidCommand = Command with { StartDate = Today.AddDays(10), EndDate = Today.AddDays(7) };

        Result<Guid> result = await _handler.Handle(invalidCommand, default);

        result.Error.Should().Be(DateRangeErrors.StartDatePrecedesEndDate);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenStartDateIsInThePast()
    {
        ReserveBookingCommand pastCommand = Command with { StartDate = Today.AddDays(-1), EndDate = Today.AddDays(2) };

        Result<Guid> result = await _handler.Handle(pastCommand, default);

        result.Error.Should().Be(BookingErrors.StartDateInPast);
        _bookingRepository.DidNotReceive().Insert(Arg.Any<Booking>());
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenOverlapping()
    {
        _bookingRepository
            .IsOverlappingAsync(Arg.Any<Apartment>(), Arg.Any<DateRange>(), Arg.Any<CancellationToken>())
            .Returns(true);

        Result<Guid> result = await _handler.Handle(Command, default);

        result.Error.Should().Be(BookingErrors.Overlap);
    }

    [Fact]
    public async Task Handle_Should_ReturnOverlap_WhenConcurrentReservationWins()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConcurrencyException("conflict", null));

        Result<Guid> result = await _handler.Handle(Command, default);

        result.Error.Should().Be(BookingErrors.Overlap);
    }

    [Fact]
    public async Task Handle_Should_InsertBookingAndSave_WhenReservationSucceeds()
    {
        Result<Guid> result = await _handler.Handle(Command, default);

        result.IsSuccess.Should().BeTrue();
        _bookingRepository.Received(1).Insert(Arg.Is<Booking>(b =>
            b.Id == result.Value &&
            b.UserId == Command.UserId &&
            b.TotalPrice.Amount == 300));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _apartment.LastBookedOnUtc.Should().Be(UtcNow);
    }
}
