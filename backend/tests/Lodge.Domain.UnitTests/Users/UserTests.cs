using Lodge.Domain.Users;
using Lodge.Domain.Users.Events;

namespace Lodge.Domain.UnitTests.Users;

public class UserTests
{
    [Fact]
    public void Create_Should_CreateUser_WhenIsValid()
    {
        // Arrange
        FirstName firstName = FirstName.Create("FirstName").Value;
        LastName lastName = LastName.Create("LastName").Value;
        Email email = Email.Create("email@email.com").Value;
        const string hash = "hash";

        // Act
        var user = User.Create(firstName, lastName, email, hash);

        // Assert
        user.Should().NotBeNull();
    }

    [Fact]
    public void Create_Should_RaiseDomainEvent_WhenIsValid()
    {
        // Arrange
        FirstName firstName = FirstName.Create("FirstName").Value;
        LastName lastName = LastName.Create("LastName").Value;
        Email email = Email.Create("email@email.com").Value;
        const string hash = "hash";

        // Act
        var user = User.Create(firstName, lastName, email, hash);

        // Assert
        user.DomainEvents
            .Should().ContainSingle()
            .Which
            .Should().BeOfType<UserCreatedDomainEvent>();
    }

    [Fact]
    public void ChangeName_Should_NotRaiseEvent_WhenNameIsUnchanged()
    {
        var user = User.Create(
            FirstName.Create("FirstName").Value,
            LastName.Create("LastName").Value,
            Email.Create("email@email.com").Value,
            "hash");
        user.ClearDomainEvents();

        user.ChangeName(FirstName.Create("FirstName").Value, LastName.Create("LastName").Value);

        user.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ChangePassword_Should_RaiseEventCarryingOnlyTheUserId()
    {
        var user = User.Create(
            FirstName.Create("FirstName").Value,
            LastName.Create("LastName").Value,
            Email.Create("email@email.com").Value,
            "hash");
        user.ClearDomainEvents();

        user.ChangePassword("new-hash");

        user.DomainEvents.Should().ContainSingle()
            .Which.Should().Be(new UserPasswordChangedDomainEvent(user.Id));
    }

    [Fact]
    public void Create_Should_DefaultToTheGuestRole()
    {
        var user = User.Create(
            FirstName.Create("FirstName").Value,
            LastName.Create("LastName").Value,
            Email.Create("email@email.com").Value,
            "hash");

        user.Role.Should().Be(UserRole.Guest);
    }

    [Fact]
    public void ChangeRole_Should_SetTheRole()
    {
        var user = User.Create(
            FirstName.Create("FirstName").Value,
            LastName.Create("LastName").Value,
            Email.Create("email@email.com").Value,
            "hash");

        user.ChangeRole(UserRole.Admin);

        user.Role.Should().Be(UserRole.Admin);
    }
}
