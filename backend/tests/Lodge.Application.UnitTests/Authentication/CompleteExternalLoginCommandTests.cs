using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Authentication.External;
using Lodge.Application.Authentication.External.Complete;
using Lodge.Application.Core.Errors;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;
using NSubstitute;

namespace Lodge.Application.UnitTests.Authentication;

/// <summary>
/// The user resolution of a social login, with a fake external identity.
/// </summary>
public class CompleteExternalLoginCommandTests
{
    private static readonly DateTime UtcNow = new(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);

    private static readonly CompleteExternalLoginCommand Command = new("google", "code", "state", null, "cookie");

    private readonly IExternalAuthenticationService _externalAuthenticationService = Substitute.For<IExternalAuthenticationService>();
    private readonly IExternalLoginRepository _externalLoginRepository = Substitute.For<IExternalLoginRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IJwtProvider _jwtProvider = Substitute.For<IJwtProvider>();
    private readonly CompleteExternalLoginCommandHandler _handler;

    public CompleteExternalLoginCommandTests()
    {
        IDateTimeProvider dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(UtcNow);

        _jwtProvider.Create(Arg.Any<User>()).Returns(call => $"token-for-{call.Arg<User>().Id}");
        _jwtProvider.CreateTwoFactorChallenge(Arg.Any<User>()).Returns("challenge");

        _handler = new CompleteExternalLoginCommandHandler(
            _externalAuthenticationService,
            _externalLoginRepository,
            _userRepository,
            _jwtProvider,
            dateTimeProvider);
    }

    [Fact]
    public async Task Should_SignIn_ByExistingExternalLogin()
    {
        User user = CreateUser("linked@lodge.dev", verified: false);
        ProviderReturns(Identity("someone-else@gmail.com"));
        _externalLoginRepository.GetAsync("google", "google-123", Arg.Any<CancellationToken>())
            .Returns(ExternalLogin.Create(user.Id, "google", "google-123", UtcNow));
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        Result<ExternalLoginResult> result = await _handler.Handle(Command, default);

        result.Value.Should().Be(new ExternalLoginResult($"token-for-{user.Id}", null, "/trips"));
        _externalLoginRepository.DidNotReceive().Insert(Arg.Any<ExternalLogin>());
        _userRepository.DidNotReceive().Insert(Arg.Any<User>());
    }

    [Fact]
    public async Task Should_LinkLogin_ToUserWithMatchingVerifiedEmail()
    {
        User user = CreateUser("jane@lodge.dev", verified: true);
        ProviderReturns(Identity("Jane@Lodge.dev"));
        _userRepository.GetByEmailAsync(Arg.Is<Email>(e => e.Value == "jane@lodge.dev"), Arg.Any<CancellationToken>())
            .Returns(user);

        Result<ExternalLoginResult> result = await _handler.Handle(Command, default);

        result.Value.Token.Should().Be($"token-for-{user.Id}");
        _externalLoginRepository.Received(1).Insert(Arg.Is<ExternalLogin>(l =>
            l.UserId == user.Id && l.Provider == "google" && l.ProviderKey == "google-123"));
        _userRepository.DidNotReceive().Insert(Arg.Any<User>());
    }

    [Fact]
    public async Task Should_NotLink_ToUserWhoseEmailIsNotVerified()
    {
        User user = CreateUser("jane@lodge.dev", verified: false);
        ProviderReturns(Identity("jane@lodge.dev"));
        _userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);

        Result<ExternalLoginResult> result = await _handler.Handle(Command, default);

        result.Error.Should().Be(ExternalAuthenticationErrors.EmailInUse);
        _externalLoginRepository.DidNotReceive().Insert(Arg.Any<ExternalLogin>());
    }

    [Fact]
    public async Task Should_CreateUser_WithVerifiedEmailAndNoPassword()
    {
        ProviderReturns(Identity("new.person@gmail.com", firstName: "New", lastName: "Person"));

        Result<ExternalLoginResult> result = await _handler.Handle(Command, default);

        result.IsSuccess.Should().BeTrue();
        _userRepository.Received(1).Insert(Arg.Is<User>(u =>
            u.Email.Value == "new.person@gmail.com" &&
            u.FirstName.Value == "New" &&
            u.LastName.Value == "Person" &&
            u.IsEmailVerified &&
            !u.HasPassword));
        _externalLoginRepository.Received(1).Insert(Arg.Is<ExternalLogin>(l => l.ProviderKey == "google-123"));
    }

    [Fact]
    public async Task Should_FallBack_ToTheEmail_WhenTheProviderHasNoName()
    {
        ProviderReturns(Identity("octocat@github.test", firstName: null, lastName: null));

        await _handler.Handle(Command, default);

        _userRepository.Received(1).Insert(Arg.Is<User>(u => u.FirstName.Value == "octocat" && u.LastName.Value == "-"));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("jane@lodge.dev", false)]
    [InlineData("not-an-email", true)]
    public async Task Should_Fail_WithoutAVerifiedEmail(string? email, bool emailVerified)
    {
        ProviderReturns(Identity(email) with { EmailVerified = emailVerified });

        Result<ExternalLoginResult> result = await _handler.Handle(Command, default);

        result.Error.Should().Be(ExternalAuthenticationErrors.EmailNotVerified);
        _userRepository.DidNotReceive().Insert(Arg.Any<User>());
    }

    [Fact]
    public async Task Should_ReturnTwoFactorChallenge_WhenTwoFactorIsEnabled()
    {
        User user = CreateUser("jane@lodge.dev", verified: true);
        user.BeginTwoFactorSetup("protected-secret");
        user.EnableTwoFactor(UtcNow, 1);
        ProviderReturns(Identity("jane@lodge.dev"));
        _userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);

        Result<ExternalLoginResult> result = await _handler.Handle(Command, default);

        result.Value.Should().Be(new ExternalLoginResult(null, "challenge", "/trips"));
    }

    [Theory]
    [InlineData("access_denied", "External.AccessDenied")]
    [InlineData("server_error", "External.ProviderError")]
    public async Task Should_Fail_WhenTheProviderReportsAnError(string providerError, string expectedCode)
    {
        Result<ExternalLoginResult> result = await _handler.Handle(Command with { ProviderError = providerError }, default);

        result.Error.Code.Should().Be(expectedCode);
        await _externalAuthenticationService.DidNotReceiveWithAnyArgs().CompleteAsync(default!, default, default, default);
    }

    [Fact]
    public async Task Should_Fail_WhenTheCallbackIsInvalid()
    {
        _externalAuthenticationService
            .CompleteAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ExternalLoginCallback>(ExternalAuthenticationErrors.InvalidState));

        Result<ExternalLoginResult> result = await _handler.Handle(Command, default);

        result.Error.Should().Be(ExternalAuthenticationErrors.InvalidState);
    }

    private static ExternalIdentity Identity(string? email, string? firstName = "Jane", string? lastName = "Doe") =>
        new("google", "google-123", email, true, firstName, lastName);

    private static User CreateUser(string email, bool verified)
    {
        var user = User.Create(
            FirstName.Create("First").Value,
            LastName.Create("Last").Value,
            Email.Create(email).Value,
            "hash");

        if (verified)
        {
            user.VerifyEmail(UtcNow);
        }

        return user;
    }

    private void ProviderReturns(ExternalIdentity identity) =>
        _externalAuthenticationService
            .CompleteAsync("google", "code", "state", "cookie", Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ExternalLoginCallback(identity, "/trips")));
}
