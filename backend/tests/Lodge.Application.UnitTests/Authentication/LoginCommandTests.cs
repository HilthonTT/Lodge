using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Authentication.Login;
using Lodge.Contracts.Authentication;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;
using NSubstitute;

namespace Lodge.Application.UnitTests.Authentication;

public class LoginCommandTests
{
    private const string Password = "AbC-123";

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHashChecker _passwordHashChecker = Substitute.For<IPasswordHashChecker>();
    private readonly IJwtProvider _jwtProvider = Substitute.For<IJwtProvider>();
    private readonly LoginCommandHandler _handler;

    public LoginCommandTests()
    {
        _passwordHashChecker.HashesMatch("hash", Password).Returns(true);
        _jwtProvider.Create(Arg.Any<User>()).Returns("access-token");
        _jwtProvider.CreateTwoFactorChallenge(Arg.Any<User>()).Returns("challenge-token");

        _handler = new LoginCommandHandler(_userRepository, _passwordHashChecker, _jwtProvider);
    }

    [Fact]
    public async Task Handle_Should_ReturnToken_WithoutTwoFactor()
    {
        User user = CreateUser("user@lodge.dev");
        _userRepository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);

        Result<LoginResponse> result = await _handler.Handle(new LoginCommand(user.Email.Value, Password), default);

        result.Value.Should().Be(new LoginResponse("access-token", false, null));
    }

    [Fact]
    public async Task Handle_Should_ReturnChallenge_WhenTwoFactorIsEnabled()
    {
        User user = CreateUser("2fa@lodge.dev");
        user.BeginTwoFactorSetup("protected-secret");
        user.EnableTwoFactor(DateTime.UtcNow, 1);
        _userRepository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);

        Result<LoginResponse> result = await _handler.Handle(new LoginCommand(user.Email.Value, Password), default);

        result.Value.Should().Be(new LoginResponse(null, true, "challenge-token"));
        _jwtProvider.DidNotReceive().Create(Arg.Any<User>());
    }

    [Fact]
    public async Task Handle_Should_FailCleanly_ForAccountsWithoutPassword()
    {
        var user = User.CreateExternal(
            FirstName.Create("Social").Value,
            LastName.Create("User").Value,
            Email.Create("social@lodge.dev").Value,
            DateTime.UtcNow);
        _userRepository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHashChecker.HashesMatch(Arg.Any<string?>(), Arg.Any<string?>()).Returns(true);

        Result<LoginResponse> result = await _handler.Handle(new LoginCommand(user.Email.Value, Password), default);

        result.Error.Should().Be(AuthenticationErrors.InvalidEmailOrPassword);
    }

    private static User CreateUser(string email) => User.Create(
        FirstName.Create("First").Value,
        LastName.Create("Last").Value,
        Email.Create(email).Value,
        "hash");
}
