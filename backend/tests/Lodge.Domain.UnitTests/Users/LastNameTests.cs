using FluentAssertions;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;

namespace Domain.UnitTests.Users;

public class LastNameTests
{
    [Fact]
    public void FirstName_Should_ReturnFailure_WhenValueIsTooLong()
    {
        // Arrange
        string invalidLastName = new('A', LastName.MaxLength + 1);

        // Act
        Result<LastName> result = LastName.Create(invalidLastName);

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Error.Should().Be(LastNameErrors.TooLong);
    }

    [Fact]
    public void FirstName_Should_ReturnFailure_WhenValueIsEmpty()
    {
        // Act
        Result<LastName> result = LastName.Create(string.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();

        result.Error.Should().Be(LastNameErrors.Empty);
    }
}
