using Lodge.Domain.Core.Abstractions;
using Lodge.Domain.Core.Guards;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users.Events;

namespace Lodge.Domain.Users;

/// <summary>
/// Represents the user entity.
/// </summary>
public sealed class User : Entity, IAuditableEntity, ISoftDeletableEntity
{
    /// <summary>
    /// The maximum length of <see cref="Bio"/>.
    /// </summary>
    public const int BioMaxLength = 500;

    // Null for accounts that were created through a social login and never set a password.
    private string? _passwordHash;

    private User(
        Guid id,
        FirstName firstName,
        LastName lastName,
        Email email,
        Guid? imageId,
        string? passwordHash)
        : base(id)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        ImageId = imageId;
        _passwordHash = passwordHash;
    }

    private User()
    {
    }

    public FirstName FirstName { get; private set; } = null!;

    public LastName LastName { get; private set; } = null!;

    public Email Email { get; private set; } = null!;

    public string FullName => $"{FirstName.Value} {LastName.Value}";

    public Guid? ImageId { get; private set; }

    public UserRole Role { get; private set; } = UserRole.Guest;

    /// <summary>
    /// Gets when the e-mail address was verified, or null when it is not verified yet.
    /// </summary>
    public DateTime? EmailVerifiedOnUtc { get; private set; }

    public bool IsEmailVerified => EmailVerifiedOnUtc is not null;

    /// <summary>
    /// Gets a value indicating whether the user can sign in with a password.
    /// </summary>
    public bool HasPassword => !string.IsNullOrEmpty(_passwordHash);

    /// <summary>
    /// Gets the short public biography.
    /// </summary>
    public string? Bio { get; private set; }

    /// <summary>
    /// Gets the encrypted TOTP secret. It is set by <see cref="BeginTwoFactorSetup"/> and only used for sign-in
    /// once <see cref="EnableTwoFactor"/> has been called.
    /// </summary>
    public string? TwoFactorSecret { get; private set; }

    public DateTime? TwoFactorEnabledOnUtc { get; private set; }

    public bool TwoFactorEnabled => TwoFactorEnabledOnUtc is not null;

    /// <summary>
    /// Gets the last TOTP time step that was accepted, so that a code cannot be replayed.
    /// </summary>
    public long? TwoFactorLastUsedStep { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the user is a Superhost (updated daily by a background job).
    /// </summary>
    public bool IsSuperhost { get; private set; }

    public DateTime CreatedOnUtc { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public DateTime? DeletedOnUtc { get; set; }

    public bool Deleted { get; set; }

    public static User Create(
        FirstName firstName,
        LastName lastName,
        Email email,
        string passwordHash,
        Guid? imageId = null)
    {
        var user = new User(Guid.NewGuid(), firstName, lastName, email, imageId, passwordHash);

        user.RaiseDomainEvent(new UserCreatedDomainEvent(user.Id));

        return user;
    }

    /// <summary>
    /// Creates a user from an external (social) login. The user has no password and a verified e-mail address,
    /// because the identity provider has verified it.
    /// </summary>
    public static User CreateExternal(FirstName firstName, LastName lastName, Email email, DateTime utcNow)
    {
        var user = new User(Guid.NewGuid(), firstName, lastName, email, null, null)
        {
            EmailVerifiedOnUtc = utcNow,
        };

        user.RaiseDomainEvent(new UserCreatedDomainEvent(user.Id));

        return user;
    }

    public bool VerifyPasswordHash(string password, IPasswordHashChecker checker) =>
        HasPassword && !string.IsNullOrWhiteSpace(password) && checker.HashesMatch(_passwordHash, password);

    /// <summary>
    /// Changes the password hash. The caller must ensure that the new password differs from the
    /// current one (see <see cref="VerifyPasswordHash"/>): salted hashes cannot be compared directly.
    /// For a user without a password, this sets one.
    /// </summary>
    public void ChangePassword(string passwordHash)
    {
        Ensure.NotNullOrEmpty(passwordHash);

        _passwordHash = passwordHash;

        RaiseDomainEvent(new UserPasswordChangedDomainEvent(Id));
    }

    public void ChangeName(FirstName firstName, LastName lastName)
    {
        if (FirstName == firstName && LastName == lastName)
        {
            return;
        }

        FirstName = firstName;
        LastName = lastName;

        RaiseDomainEvent(new UserNameChangedDomainEvent(Id));
    }

    public void ChangeRole(UserRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown user role.");
        }

        Role = role;
    }

    public void ChangeImage(Guid imageId)
    {
        if (imageId == Guid.Empty)
        {
            throw new ArgumentException("The image identifier is required.", nameof(imageId));
        }

        ImageId = imageId;
    }

    public void RemoveImage() => ImageId = null;

    /// <summary>
    /// Grants or revokes the Superhost status.
    /// </summary>
    public void SetSuperhost(bool isSuperhost) => IsSuperhost = isSuperhost;

    /// <summary>
    /// Changes the biography. Blank values clear it.
    /// </summary>
    public Result ChangeBio(string? bio)
    {
        string? normalized = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();

        if (normalized?.Length > BioMaxLength)
        {
            return Result.Failure(UserErrors.BioTooLong);
        }

        Bio = normalized;

        return Result.Success();
    }

    /// <summary>
    /// Marks the e-mail address as verified. Does nothing when it is already verified.
    /// </summary>
    public void VerifyEmail(DateTime utcNow)
    {
        EmailVerifiedOnUtc ??= utcNow;
    }

    /// <summary>
    /// Stores a new (encrypted) TOTP secret that still has to be confirmed with <see cref="EnableTwoFactor"/>.
    /// </summary>
    public Result BeginTwoFactorSetup(string protectedSecret)
    {
        Ensure.NotNullOrEmpty(protectedSecret);

        if (TwoFactorEnabled)
        {
            return Result.Failure(TwoFactorErrors.AlreadyEnabled);
        }

        TwoFactorSecret = protectedSecret;
        TwoFactorLastUsedStep = null;

        return Result.Success();
    }

    /// <summary>
    /// Enables two-factor authentication once the user has proven possession of the secret.
    /// </summary>
    /// <param name="utcNow">The current time.</param>
    /// <param name="usedStep">The time step of the TOTP code that confirmed the setup.</param>
    public Result EnableTwoFactor(DateTime utcNow, long usedStep)
    {
        if (TwoFactorEnabled)
        {
            return Result.Failure(TwoFactorErrors.AlreadyEnabled);
        }

        if (TwoFactorSecret is null)
        {
            return Result.Failure(TwoFactorErrors.NotSetUp);
        }

        TwoFactorEnabledOnUtc = utcNow;
        TwoFactorLastUsedStep = usedStep;

        return Result.Success();
    }

    public void DisableTwoFactor()
    {
        TwoFactorSecret = null;
        TwoFactorEnabledOnUtc = null;
        TwoFactorLastUsedStep = null;
    }

    /// <summary>
    /// Records the time step of an accepted TOTP code (codes of this or earlier steps are rejected afterwards).
    /// </summary>
    public void RecordTwoFactorStep(long step)
    {
        if (TwoFactorLastUsedStep is null || step > TwoFactorLastUsedStep)
        {
            TwoFactorLastUsedStep = step;
        }
    }
}
