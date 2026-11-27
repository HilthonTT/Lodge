using Lodge.Application.Abstractions.Authentication;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Authentication;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Application.Authentication.External.GetProviders;

internal sealed class GetExternalProvidersQueryHandler(IExternalAuthenticationService externalAuthenticationService)
    : IQueryHandler<GetExternalProvidersQuery, IReadOnlyList<ExternalProviderResponse>>
{
    public Task<Result<IReadOnlyList<ExternalProviderResponse>>> Handle(
        GetExternalProvidersQuery request,
        CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(externalAuthenticationService.GetProviders()));
}
