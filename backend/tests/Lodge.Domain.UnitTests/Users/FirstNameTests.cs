using FluentAssertions;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Domain.UnitTests.Users;

public class FirstNameTests
{
    [Fact]
    public void FirstName_Should_ReturnFailure_WhenValueIsTooLong()
    {
        // Arrange
        string invalidFirstName = new('A', FirstName.MaxLength + 1);

        // Act
        Result<FirstName> result = FirstName.Create(invalidFirstName);

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Error.Should().Be(FirstNameErrors.TooLong);
    }

    [Fact]
    public void FirstName_Should_ReturnFailure_WhenValueIsEmpty()
    {
        // Act
        Result<FirstName> result = FirstName.Create(string.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Error.Should().Be(FirstNameErrors.Empty);
    }
}
