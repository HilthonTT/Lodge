using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Authentication;
using Lodge.Application.Authentication.ResendVerification;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;
using NSubstitute;

namespace Lodge.Application.UnitTests.Users;

public class ResendVerificationEmailCommandTests
{
    private readonly User _user = User.Create(
        FirstName.Create("First").Value,
        LastName.Create("Last").Value,
        Email.Create("user@lodge.dev").Value,
        "hash");

    private readonly IAccountTokenService _accountTokenService = Substitute.For<IAccountTokenService>();
    private readonly ResendVerificationEmailCommandHandler _handler;

    public ResendVerificationEmailCommandTests()
    {
        IUserIdentifierProvider userIdentifierProvider = Substitute.For<IUserIdentifierProvider>();
        userIdentifierProvider.UserId.Returns(_user.Id);

        IUserRepository userRepository = Substitute.For<IUserRepository>();
        userRepository.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);

        _handler = new ResendVerificationEmailCommandHandler(userIdentifierProvider, userRepository, _accountTokenService);
    }

    [Fact]
    public async Task Handle_Should_SendTheEmail_WhenNotThrottled()
    {
        Result result = await _handler.Handle(new ResendVerificationEmailCommand(), default);

        result.IsSuccess.Should().BeTrue();
        await _accountTokenService.Received(1).SendEmailVerificationAsync(_user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_BeThrottled_WithinAMinute()
    {
        _accountTokenService
            .WasIssuedRecentlyAsync(_user.Id, UserTokenPurpose.EmailVerification, Arg.Any<CancellationToken>())
            .Returns(true);

        Result result = await _handler.Handle(new ResendVerificationEmailCommand(), default);

        result.Error.Should().Be(UserErrors.VerificationEmailThrottled);
        result.Error.Type.Should().Be(ErrorType.TooManyRequests);
        await _accountTokenService.DidNotReceiveWithAnyArgs().SendEmailVerificationAsync(default!, default);
    }
}
