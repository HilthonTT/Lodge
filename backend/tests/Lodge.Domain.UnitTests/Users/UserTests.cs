using Lodge.Domain.Users;
using FluentAssertions;
using Lodge.Domain.Users.Events;

namespace Domain.UnitTests.Users;

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
}
