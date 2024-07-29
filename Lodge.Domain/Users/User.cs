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
    private string _passwordHash;

    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// </summary>
    /// <param name="firstName">The user first name.</param>
    /// <param name="lastName">The user last name.</param>
    /// <param name="email">The user email instance.</param>
    /// <param name="passwordHash">The user password hash.</param>
    private User(
        Guid id,
        FirstName firstName, 
        LastName lastName, 
        Email email, 
        Guid? imageId,
        string passwordHash)
        : base(id)
    {
        Ensure.NotNullOrEmpty(firstName.Value, "The first name is required.");
        Ensure.NotNullOrEmpty(lastName.Value, "The last name is required.");
        Ensure.NotNullOrEmpty(email, "The email is required.");
        Ensure.NotNullOrEmpty(passwordHash, "The password hash is required");

        FirstName = firstName;
        LastName = lastName;
        Email = email;
        ImageId = imageId;
        _passwordHash = passwordHash;
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// </summary>
    /// <remarks>
    /// Required by EF Core.
    /// </remarks>
    private User()
    {
    }

    public FirstName FirstName { get; private set; }

    public LastName LastName { get; private set; }

    public Email Email { get; private set; }

    public string FullName => $"{FirstName.Value} {LastName.Value}";

    public Guid? ImageId { get; private set; }

    public DateTime CreatedOnUtc { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public DateTime? DeletedOnUtc { get; set; }
    
    public bool Deleted { get; set; }

    /// <summary>
    /// Creates a new user with the specified first name, last name, email and password hash.
    /// </summary>
    /// <param name="firstName">The first name.</param>
    /// <param name="lastName">The last name.</param>
    /// <param name="email">The email.</param>
    /// <param name="passwordHash">The password hash.</param>
    /// <returns>The newly created user instance.</returns>
    public static User Create(
        FirstName firstName, 
        LastName lastName, 
        Email email, 
        string passwordHash, 
        Guid? imageId = null)
    {
        var user = new User(
            Guid.NewGuid(),
            firstName,
            lastName,
            email,
            imageId,
            passwordHash);

        user.RaiseDomainEvent(new UserCreatedDomainEvent(user.Id));

        return user;
    }

    /// <summary>
    /// Verifies that the provided password hash matches the password hash.
    /// </summary>
    /// <param name="password">The password to be checked against the user password hash.</param>
    /// <param name="checker">The password hash checker.</param>
    /// <returns>True if the password hashes match, otherwise false.</returns>
    public bool VerifyPasswordHash(string password, IPasswordHashChecker checker) =>
        !string.IsNullOrWhiteSpace(password) && checker.HashesMatch(_passwordHash, password);

    public Result ChangePassword(string passwordHash)
    {
        if (passwordHash == _passwordHash)
        {
            return Result.Failure(UserErrors.CannotChangePassword);
        }

        _passwordHash = passwordHash;

        RaiseDomainEvent(new UserPasswordChangedDomainEvent(this));

        return Result.Success();
    }

    /// <summary>
    /// Changes the users first and last name.
    /// </summary>
    /// <param name="firstName">The new first name.</param>
    /// <param name="lastName">The new last name.</param>
    public void ChangeName(FirstName firstName, LastName lastName)
    {
        Ensure.NotNullOrEmpty(firstName.Value, "The first name is required.");
        Ensure.NotNullOrEmpty(lastName.Value, "The last name is required.");

        FirstName = firstName;

        LastName = lastName;

        RaiseDomainEvent(new UserNameChangedDomainEvent(this));
    }
}
