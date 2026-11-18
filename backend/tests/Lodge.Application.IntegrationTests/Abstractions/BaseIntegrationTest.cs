using Bogus;
using Lodge.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Application.IntegrationTests.Abstractions;

public abstract class BaseIntegrationTest : IClassFixture<IntegrationTestWebFactory>, IDisposable
{
    private readonly IServiceScope _scope;

    protected BaseIntegrationTest(IntegrationTestWebFactory factory)
    {
        _scope = factory.Services.CreateScope();
        Sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        LodgeDbContext = _scope.ServiceProvider.GetRequiredService<LodgeDbContext>();
        Faker = new();
    }

    protected ISender Sender { get; }

    protected Faker Faker { get; }

    protected LodgeDbContext LodgeDbContext { get; }
    
    public void Dispose()
    {
        _scope.Dispose();
    }
}
