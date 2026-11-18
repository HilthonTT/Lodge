using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Payments;

namespace Lodge.Application.Payments;

/// <summary>
/// Gets the static display exchange rates (<c>GET /currencies</c>).
/// </summary>
public sealed record GetCurrenciesQuery : IQuery<CurrenciesResponse>;
