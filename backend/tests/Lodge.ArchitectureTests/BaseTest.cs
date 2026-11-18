using Lodge.Application.Abstractions.Messaging;
using Lodge.Domain.Users;
using Lodge.Infrastructure.Caching;
using Lodge.Persistence;
using Lodge.Presentation.Endpoints;
using System.Reflection;

namespace ArchitectureTests;

public abstract class BaseTest
{
    protected static readonly Assembly DomainAssembly = typeof(User).Assembly;
    protected static readonly Assembly ApplicationAssembly = typeof(ICommand).Assembly;
    protected static readonly Assembly InfrastructureAssembly = typeof(CacheService).Assembly;
    protected static readonly Assembly PersistenceAssembly = typeof(LodgeDbContext).Assembly;
    protected static readonly Assembly PresentationAssembly = typeof(IEndpoint).Assembly;
}
