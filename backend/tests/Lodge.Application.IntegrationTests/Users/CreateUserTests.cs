using Lodge.Application.IntegrationTests.Abstractions;
using Lodge.Application.Users.Create;
using Lodge.Contracts.Authentication;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Lodge.Application.IntegrationTests.Users;

public class CreateUserTests(IntegrationTestWebFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_Should_AddUserToDatabase_WhenCommandIsValid()
    {
        Guid userId = await CreateAndSignInUserAsync();

        User? user = await DbContext.Users.FindAsync([userId], TestContext.Current.CancellationToken);

        user.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_Should_TreatEmailsCaseInsensitively()
    {
        string email = $"{Guid.NewGuid():N}@Lodge.Test";

        Result<TokenResponse> first = await Sender.Send(new CreateUserCommand("Ada", "Lovelace", email, Password));
        Result<TokenResponse> second = await Sender.Send(
            new CreateUserCommand("Ada", "Lovelace", email.ToUpperInvariant(), Password));

        first.IsSuccess.Should().BeTrue();
        second.Error.Should().Be(UserErrors.DuplicateEmail);
        (await DbContext.Users.CountAsync(u => u.Email.Value == email.ToLowerInvariant())).Should().Be(1);
    }
}
