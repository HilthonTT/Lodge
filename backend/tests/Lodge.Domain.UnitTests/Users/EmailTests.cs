using FluentAssertions;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Domain.UnitTests.Users;

public class EmailTests
{
    [Fact]
    public void Email_Should_ReturnFailure_WhenValueIsEmpty()
    {
        // Act
        Result<Email> result = Email.Create(string.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Error.Should().Be(EmailErrors.Empty);
    }

    [Fact]
    public void Email_Should_ReturnFailure_WhenValueIsInvalid()
    {
        // Act
        Result<Email> result = Email.Create("not-an-email");

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Error.Should().Be(EmailErrors.InvalidFormat);
    }

    [Fact]
    public void Email_Should_ReturnFailure_WhenValueIsTooLong()
    {
        // Arrange
        string invalidEmail = new('A', Email.MaxLength + 1);

        // Act
        Result<Email> result = Email.Create(invalidEmail);

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Error.Should().Be(EmailErrors.TooLong);
    }
}
