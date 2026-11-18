using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Authentication;

namespace Lodge.Application.Authentication.External;

/// <summary>
/// Lists the configured external (social) login providers.
/// </summary>
public sealed record GetExternalProvidersQuery : IQuery<IReadOnlyList<ExternalProviderResponse>>;
