using Bogus;
using Lodge.Application.Users.Create;
using Lodge.Contracts.Authentication;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Users;
using Lodge.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Lodge.Application.IntegrationTests.Abstractions;

[Collection(nameof(IntegrationTestCollection))]
public abstract class BaseIntegrationTest : IDisposable
{
    protected const string Password = "AbC-123";

    private readonly IServiceScope _scope;

    protected BaseIntegrationTest(IntegrationTestWebFactory factory)
    {
        Factory = factory;
        _scope = factory.Services.CreateScope();
        Sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        DbContext = _scope.ServiceProvider.GetRequiredService<LodgeDbContext>();
    }

    protected IntegrationTestWebFactory Factory { get; }

    protected ISender Sender { get; }

    protected Faker Faker { get; } = new();

    protected LodgeDbContext DbContext { get; }

    /// <summary>
    /// Registers a new user and makes it the current user of the test. By default the e-mail address is marked as
    /// verified (bookings require a verified e-mail).
    /// </summary>
    protected async Task<Guid> CreateAndSignInUserAsync(bool verifyEmail = true)
    {
        Result<TokenResponse> result = await Sender.Send(new CreateUserCommand(
            Faker.Name.FirstName(),
            Faker.Name.LastName(),
            $"{Guid.NewGuid():N}@lodge.test",
            Password));

        result.IsSuccess.Should().BeTrue();

        Guid userId = Guid.Parse(new JsonWebToken(result.Value.Token).GetClaim("userId").Value);
        Factory.CurrentUser.UserId = userId;

        if (verifyEmail)
        {
            User user = await DbContext.Users.SingleAsync(u => u.Id == userId);
            user.VerifyEmail(DateTime.UtcNow);
            await DbContext.SaveChangesAsync();
        }

        return userId;
    }

    public void Dispose()
    {
        _scope.Dispose();
        GC.SuppressFinalize(this);
    }
}
