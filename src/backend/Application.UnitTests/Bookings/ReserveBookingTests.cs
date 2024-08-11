using FluentAssertions;
using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Bookings.Reserve;
using Lodge.Domain.Apartements;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Shared;
using Lodge.Domain.Users;
using MediatR;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace Application.UnitTests.Bookings;

public class ReserveBookingTests
{
    private static readonly ReserveBookingCommand Command = new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        DateOnly.Parse("01-08-2024"),
        DateOnly.Parse("10-08-2024"));

    private static readonly Apartment Apartment = Apartment.Create(
        Name.Create("Name").Value,
        Description.Create("Description").Value,
        new Address("Country", "State", "ZipCode", "City", "Street"),
        Money.Zero(),
        Money.Zero(),
        5,
        5,
        "ImageUrl",
        []);

    private readonly ReserveBookingCommandHandler _handler;
    private readonly IUserIdentifierProvider _userIdentifierProviderMock;
    private readonly IApartmentRepository _apartmentRepositoryMock;
    private readonly IBookingRepository _bookingRepositoryMock;
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly PricingService _pricingServiceMock;
    private readonly IDateTimeProvider _dateTimeProviderMock;
    private readonly IPublisher _publisherMock;

    public ReserveBookingTests()
    {
        _userIdentifierProviderMock = Substitute.For<IUserIdentifierProvider>();
        _apartmentRepositoryMock = Substitute.For<IApartmentRepository>();
        _bookingRepositoryMock = Substitute.For<IBookingRepository>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _pricingServiceMock = new();
        _dateTimeProviderMock = Substitute.For<IDateTimeProvider>();
        _publisherMock = Substitute.For<IPublisher>();

        _handler = new ReserveBookingCommandHandler(
            _userIdentifierProviderMock,
            _apartmentRepositoryMock,
            _bookingRepositoryMock,
            _unitOfWorkMock,
            _pricingServiceMock,
            _dateTimeProviderMock,
            _publisherMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenUserLacksPermission()
    {
        // Arrange
        _userIdentifierProviderMock.UserId
            .Returns(Guid.NewGuid());

        _apartmentRepositoryMock.GetByIdAsync(Command.ApartmentId, default)
            .Returns(Apartment);

        _bookingRepositoryMock.IsOverlappingAsync(Arg.Any<Apartment>(), Arg.Any<DateRange>(), default)
            .Returns(false);

        // Act
        Result<Guid> result = await _handler.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserErrors.InvalidPermissions);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenApartmentIsNull()
    {
        // Arrange
        _userIdentifierProviderMock.UserId
            .Returns(Command.UserId);

        _apartmentRepositoryMock.GetByIdAsync(Command.ApartmentId, default)
            .ReturnsNull();

        _bookingRepositoryMock.IsOverlappingAsync(Arg.Any<Apartment>(), Arg.Any<DateRange>(), default)
            .Returns(false);

        // Act
        Result<Guid> result = await _handler.Handle(Command, default);

        // Assert
        result.Error.Should().Be(ApartmentErrors.NotFound(Command.ApartmentId));
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenDateRangeIsInvalid()
    {
        // Arrange
        ReserveBookingCommand invalidCommand = Command with 
        { 
            StartDate = DateOnly.Parse("10-08-2024"), 
            EndDate = DateOnly.Parse("01-08-2024")
        };

        _userIdentifierProviderMock.UserId
            .Returns(invalidCommand.UserId);

        _apartmentRepositoryMock.GetByIdAsync(invalidCommand.ApartmentId, default)
            .Returns(Apartment);

        _bookingRepositoryMock.IsOverlappingAsync(Arg.Any<Apartment>(), Arg.Any<DateRange>(), default)
            .Returns(false);

        // Act
        Result<Guid> result = await _handler.Handle(invalidCommand, default);

        // Assert
        result.Error.Should().Be(DateRangeErrors.StartDatePrecedesEndDate);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenOverlapping()
    {
        // Arrange
        _userIdentifierProviderMock.UserId
            .Returns(Command.UserId);

        _apartmentRepositoryMock.GetByIdAsync(Command.ApartmentId, default)
            .Returns(Apartment);

        _bookingRepositoryMock.IsOverlappingAsync(Arg.Any<Apartment>(), Arg.Any<DateRange>(), default)
            .Returns(true);

        // Act
        Result<Guid> result = await _handler.Handle(Command, default);

        // Assert
        result.Error.Should().Be(BookingErrors.Overlap);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WhenCreateSucceeds()
    {
        // Arrange
        _userIdentifierProviderMock.UserId
            .Returns(Command.UserId);

        _apartmentRepositoryMock.GetByIdAsync(Command.ApartmentId, default)
            .Returns(Apartment);

        _bookingRepositoryMock.IsOverlappingAsync(Arg.Any<Apartment>(), Arg.Any<DateRange>(), default)
            .Returns(false);

        // Act
        Result<Guid> result = await _handler.Handle(Command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_CallRepository_WhenCreateSucceeds()
    {
        // Arrange
        _userIdentifierProviderMock.UserId
           .Returns(Command.UserId);

        _apartmentRepositoryMock.GetByIdAsync(Command.ApartmentId, default)
            .Returns(Apartment);

        _bookingRepositoryMock.IsOverlappingAsync(Arg.Any<Apartment>(), Arg.Any<DateRange>(), default)
            .Returns(false);

        // Act
        Result<Guid> result = await _handler.Handle(Command, default);

        // Assert
        _bookingRepositoryMock.Received(1).Insert(Arg.Is<Booking>(b => b.Id == result.Value));
    }

    [Fact]
    public async Task Handle_Should_CallUnitOfWork_WhenCreateSucceeds()
    {
        // Arrange
        _userIdentifierProviderMock.UserId
           .Returns(Command.UserId);

        _apartmentRepositoryMock.GetByIdAsync(Command.ApartmentId, default)
            .Returns(Apartment);

        _bookingRepositoryMock.IsOverlappingAsync(Arg.Any<Apartment>(), Arg.Any<DateRange>(), default)
            .Returns(false);

        // Act
        await _handler.Handle(Command, default);

        // Assert
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_CallPublisher_WhenCreateSucceeds()
    {
        // Arrange
        _userIdentifierProviderMock.UserId
           .Returns(Command.UserId);

        _apartmentRepositoryMock.GetByIdAsync(Command.ApartmentId, default)
            .Returns(Apartment);

        _bookingRepositoryMock.IsOverlappingAsync(Arg.Any<Apartment>(), Arg.Any<DateRange>(), default)
            .Returns(false);

        // Act
        await _handler.Handle(Command, default);

        // Assert
        await _publisherMock.Received(1).Publish(Arg.Any<INotification>());
    }
}
