using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Cryptography;
using Lodge.Application.Users.ChangePassword;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;
using NSubstitute;

namespace Lodge.Application.UnitTests.Users;

public class ChangePasswordCommandTests
{
    private const string CurrentPassword = "Curr3nt-Password";
    private const string NewPassword = "N3w-Password";

    private readonly User _user = User.Create(
        FirstName.Create("First").Value,
        LastName.Create("Last").Value,
        Email.Create("user@lodge.dev").Value,
        "current-hash");

    private readonly IUserIdentifierProvider _userIdentifierProvider = Substitute.For<IUserIdentifierProvider>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IPasswordHashChecker _passwordHashChecker = Substitute.For<IPasswordHashChecker>();
    private readonly ChangePasswordCommandHandler _handler;

    public ChangePasswordCommandTests()
    {
        _userIdentifierProvider.UserId.Returns(_user.Id);
        _userRepository.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);
        _passwordHasher.HashPassword(Arg.Any<Password>()).Returns("new-hash");

        _handler = new ChangePasswordCommandHandler(
            _userIdentifierProvider,
            _userRepository,
            _passwordHasher,
            _passwordHashChecker);
    }

    [Fact]
    public async Task Handle_Should_Fail_WhenNewPasswordEqualsCurrentPassword()
    {
        // Regression: salted hashes were compared, so "same password" was never detected.
        _passwordHashChecker.HashesMatch("current-hash", CurrentPassword).Returns(true);
        _passwordHashChecker.HashesMatch("current-hash", NewPassword).Returns(true);

        Result result = await _handler.Handle(new ChangePasswordCommand(_user.Id, CurrentPassword, NewPassword), default);

        result.Error.Should().Be(UserErrors.CannotChangePassword);
    }

    [Fact]
    public async Task Handle_Should_ChangePassword_WhenPasswordIsNew()
    {
        _passwordHashChecker.HashesMatch("current-hash", CurrentPassword).Returns(true);

        Result result = await _handler.Handle(new ChangePasswordCommand(_user.Id, CurrentPassword, NewPassword), default);

        result.IsSuccess.Should().BeTrue();
        _user.VerifyPasswordHash(NewPassword, new HashEqualsChecker("new-hash")).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Fail_WhenCurrentPasswordIsWrong()
    {
        // Regression: a stolen access token was enough to set a new password.
        Result result = await _handler.Handle(new ChangePasswordCommand(_user.Id, "wrong", NewPassword), default);

        result.Error.Should().Be(UserErrors.InvalidCurrentPassword);
    }

    [Fact]
    public async Task Handle_Should_Fail_ForAnotherUser()
    {
        Result result = await _handler.Handle(new ChangePasswordCommand(Guid.NewGuid(), CurrentPassword, NewPassword), default);

        result.Error.Should().Be(UserErrors.InvalidPermissions);
    }

    private sealed class HashEqualsChecker(string expectedHash) : IPasswordHashChecker
    {
        public bool HashesMatch(string? passwordHash, string? providedPassword) => passwordHash == expectedHash;
    }
}
