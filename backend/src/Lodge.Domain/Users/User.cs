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
    private string _passwordHash = string.Empty;

    private User(
        Guid id,
        FirstName firstName,
        LastName lastName,
        Email email,
        Guid? imageId,
        string passwordHash)
        : base(id)
    {
        Ensure.NotNullOrEmpty(passwordHash);

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

    public bool VerifyPasswordHash(string password, IPasswordHashChecker checker) =>
        !string.IsNullOrWhiteSpace(password) && checker.HashesMatch(_passwordHash, password);

    /// <summary>
    /// Changes the password hash. The caller must ensure that the new password differs from the
    /// current one (see <see cref="VerifyPasswordHash"/>): salted hashes cannot be compared directly.
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
}
