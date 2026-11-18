using Lodge.Domain.Users;

namespace Lodge.Domain.UnitTests.Users;

public class AccountTests
{
    private static readonly DateTime UtcNow = new(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CreateExternal_Should_CreateAVerifiedUserWithoutPassword()
    {
        var user = User.CreateExternal(
            FirstName.Create("Jane").Value,
            LastName.Create("Doe").Value,
            Email.Create("jane@lodge.dev").Value,
            UtcNow);

        user.HasPassword.Should().BeFalse();
        user.IsEmailVerified.Should().BeTrue();
        user.EmailVerifiedOnUtc.Should().Be(UtcNow);
        user.VerifyPasswordHash("anything", new AlwaysMatches()).Should().BeFalse();
    }

    [Fact]
    public void ChangePassword_Should_SetAPassword_ForUsersWithoutOne()
    {
        var user = User.CreateExternal(
            FirstName.Create("Jane").Value,
            LastName.Create("Doe").Value,
            Email.Create("jane@lodge.dev").Value,
            UtcNow);

        user.ChangePassword("new-hash");

        user.HasPassword.Should().BeTrue();
    }

    [Fact]
    public void VerifyEmail_Should_KeepTheFirstVerificationTime()
    {
        User user = CreateUser();

        user.IsEmailVerified.Should().BeFalse();
        user.VerifyEmail(UtcNow);
        user.VerifyEmail(UtcNow.AddDays(1));

        user.EmailVerifiedOnUtc.Should().Be(UtcNow);
    }

    [Fact]
    public void ChangeBio_Should_TrimAndValidateTheLength()
    {
        User user = CreateUser();

        user.ChangeBio("  Hello  ").IsSuccess.Should().BeTrue();
        user.Bio.Should().Be("Hello");

        user.ChangeBio(new string('a', User.BioMaxLength + 1)).Error.Should().Be(UserErrors.BioTooLong);
        user.Bio.Should().Be("Hello");

        user.ChangeBio("   ").IsSuccess.Should().BeTrue();
        user.Bio.Should().BeNull();
    }

    [Fact]
    public void TwoFactor_Should_OnlyBeEnabled_AfterSetup()
    {
        User user = CreateUser();

        user.EnableTwoFactor(UtcNow, 1).Error.Should().Be(TwoFactorErrors.NotSetUp);

        user.BeginTwoFactorSetup("secret").IsSuccess.Should().BeTrue();
        user.TwoFactorEnabled.Should().BeFalse();

        user.EnableTwoFactor(UtcNow, 10).IsSuccess.Should().BeTrue();
        user.TwoFactorEnabled.Should().BeTrue();
        user.TwoFactorLastUsedStep.Should().Be(10);

        user.BeginTwoFactorSetup("other").Error.Should().Be(TwoFactorErrors.AlreadyEnabled);

        user.RecordTwoFactorStep(9);
        user.TwoFactorLastUsedStep.Should().Be(10);

        user.DisableTwoFactor();
        user.TwoFactorEnabled.Should().BeFalse();
        user.TwoFactorSecret.Should().BeNull();
    }

    [Fact]
    public void UserToken_Should_BeUsableOnce_UntilItExpires()
    {
        var token = UserToken.Create(Guid.NewGuid(), UserTokenPurpose.PasswordReset, "hash", UtcNow, TimeSpan.FromHours(1));

        token.IsUsable(UtcNow.AddMinutes(59)).Should().BeTrue();
        token.IsUsable(UtcNow.AddHours(1)).Should().BeFalse();

        token.Consume(UtcNow.AddMinutes(1));
        token.IsUsable(UtcNow.AddMinutes(2)).Should().BeFalse();
    }

    [Fact]
    public void RecoveryCode_Should_BeSingleUse()
    {
        var code = RecoveryCode.Create(Guid.NewGuid(), "hash");

        code.IsUsed.Should().BeFalse();
        code.Use(UtcNow);
        code.IsUsed.Should().BeTrue();
    }

    private static User CreateUser() => User.Create(
        FirstName.Create("First").Value,
        LastName.Create("Last").Value,
        Email.Create("user@lodge.dev").Value,
        "hash");

    private sealed class AlwaysMatches : IPasswordHashChecker
    {
        public bool HashesMatch(string? passwordHash, string? providedPassword) => true;
    }
}
