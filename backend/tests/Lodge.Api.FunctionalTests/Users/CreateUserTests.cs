using Api.FunctionalTests.Abstractions;
using Lodge.Contracts.Authentication;
using System.Net.Http.Json;
using FluentAssertions;
using Api.FunctionalTests.Contracts;
using Api.FunctionalTests.Extensions;
using System.Net;
using Lodge.Application.Core.Errors;

namespace Api.FunctionalTests.Users;

public class CreateUserTests : BaseFunctionalTest
{
    private const string RequestUri = "api/v1/authentication/register";

    private static readonly RegisterRequest Request = new(
        "FirstName",
        "LastName",
        "email@email.com",
        "AbC-123");

    public CreateUserTests(FunctionalTestWebAppFactory factory) 
        : base(factory)
    {
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenEmailIsMissing()
    {
        // Arrange
        var request = Request with { Email = "" };

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(RequestUri, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        CustomProblemDetails problemDetails = await response.GetProblemDetailsAsync();

        problemDetails.Errors.Select(e => e.Code)
            .Should()
            .Contain([
                ValidationErrors.CreateUser.EmailIsRequired.Code,
                ValidationErrors.CreateUser.EmailMustBeARealEmail.Code,
            ]);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenEmailIsInvalid()
    {
        // Arrange
        var request = Request with { Email = "invalid_email" };

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(RequestUri, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        CustomProblemDetails problemDetails = await response.GetProblemDetailsAsync();

        problemDetails.Errors.Select(e => e.Code)
            .Should()
            .Contain([
                ValidationErrors.CreateUser.EmailMustBeARealEmail.Code,
            ]);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenFirstNameIsMissing()
    {
        // Arrange
        var request = Request with { FirstName = "" };

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(RequestUri, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        CustomProblemDetails problemDetails = await response.GetProblemDetailsAsync();

        problemDetails.Errors.Select(e => e.Code)
            .Should()
            .Contain([
                ValidationErrors.CreateUser.FirstNameIsRequired.Code,
            ]);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenLastNameIsMissing()
    {
        // Arrange
        var request = Request with { LastName = "" };

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(RequestUri, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        CustomProblemDetails problemDetails = await response.GetProblemDetailsAsync();

        problemDetails.Errors.Select(e => e.Code)
            .Should()
            .Contain([
                ValidationErrors.CreateUser.LastNameIsRequired.Code,
            ]);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenPasswordIsMissing()
    {
        // Arrange
        var request = Request with { Password = "" };

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(RequestUri, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        CustomProblemDetails problemDetails = await response.GetProblemDetailsAsync();

        problemDetails.Errors.Select(e => e.Code)
            .Should()
            .Contain([
                ValidationErrors.CreateUser.PasswordIsRequired.Code,
            ]);
    }

    [Fact]
    public async Task Should_ReturnOk_WhenRequestIsValid()
    {
        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(RequestUri, Request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Should_ReturnConflict_WhenUserExists()
    {
        // Act
        await HttpClient.PostAsJsonAsync(RequestUri, Request);

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(RequestUri, Request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
