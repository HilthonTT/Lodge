using FluentAssertions;
using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Cryptography;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Users.Create;
using Lodge.Contracts.Authentication;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;
using NSubstitute;

namespace Application.UnitTests.Users;

public class CreateUserCommandTests
{
    private static readonly CreateUserCommand Command = new(
        "FirstName", 
        "LastName", 
        "email@email.com",
        "AbC-123");

    private readonly CreateUserCommandHandler _handler;

    private readonly IUserRepository _userRepositoryMock;
    private readonly IPasswordHasher _passwordHasherMock;
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly IJwtProvider _jwtProviderMock;

    public CreateUserCommandTests()
    {
        _userRepositoryMock = Substitute.For<IUserRepository>();
        _passwordHasherMock = Substitute.For<IPasswordHasher>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _jwtProviderMock = Substitute.For<IJwtProvider>();

        _handler = new CreateUserCommandHandler(
            _userRepositoryMock,
            _passwordHasherMock,
            _unitOfWorkMock,
            _jwtProviderMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnError_WhenEmailIsInvalid()
    {
        // Arrange
        CreateUserCommand invalidCommand = Command with { Email = "bad_email" };

        // Act
        Result<TokenResponse> result = await _handler.Handle(invalidCommand, default);

        // Assert
        result.Error.Should().Be(EmailErrors.InvalidFormat);
    }

    [Fact]
    public async Task Handle_Should_ReturnError_WhenEmailIsNotUnique()
    {
        // Arrange
        _userRepositoryMock.IsEmailUniqueAsync(Arg.Is<Email>(e => e.Value == Command.Email))
            .Returns(false);

        // Act
        Result<TokenResponse> result = await _handler.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserErrors.DuplicateEmail);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WhenCreateSucceeds()
    {
        // Arrange
        _passwordHasherMock.HashPassword(Arg.Is<Password>(p => p.Value == Command.Password))
            .Returns("SomeHashAbc123");

        _userRepositoryMock.IsEmailUniqueAsync(Arg.Is<Email>(e => e.Value == Command.Email))
            .Returns(true);

        // Act
        Result<TokenResponse> result = await _handler.Handle(Command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_CallRepository_WhenCreateSucceeds()
    {
        // Arrange
        _passwordHasherMock.HashPassword(Arg.Is<Password>(p => p.Value == Command.Password))
           .Returns("SomeHashAbc123");

        _userRepositoryMock.IsEmailUniqueAsync(Arg.Is<Email>(e => e.Value == Command.Email))
            .Returns(true);

        // Act
        await _handler.Handle(Command, default);

        // Assert
        _userRepositoryMock.Received(1).Insert(Arg.Is<User>(u => u.Email == Command.Email));
    }

    [Fact]
    public async Task Handle_Should_CallUnitOfWork_WhenCreateSucceeds()
    {
        // Arrange
        _passwordHasherMock.HashPassword(Arg.Is<Password>(p => p.Value == Command.Password))
            .Returns("SomeHashAbc123");

        _userRepositoryMock.IsEmailUniqueAsync(Arg.Is<Email>(e => e.Value == Command.Email))
             .Returns(true);

        // Act
        await _handler.Handle(Command, default);

        // Assert
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_CallJwtProvider_WhenCreateSucceeds()
    {
        // Arrange
        _passwordHasherMock.HashPassword(Arg.Is<Password>(p => p.Value == Command.Password))
           .Returns("SomeHashAbc123");

        _userRepositoryMock.IsEmailUniqueAsync(Arg.Is<Email>(e => e.Value == Command.Email))
             .Returns(true);

        // Act
        await _handler.Handle(Command, default);

        // Assert
        _jwtProviderMock.Received(1).Create(Arg.Is<User>(u => u.Email == Command.Email));
    }
}
