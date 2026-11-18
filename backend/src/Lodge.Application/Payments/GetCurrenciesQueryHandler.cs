using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Payments;
using Lodge.Domain.Core.Primitives;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Payments;

internal sealed class GetCurrenciesQueryHandler(IOptions<CurrencyOptions> options)
    : IQueryHandler<GetCurrenciesQuery, CurrenciesResponse>
{
    public Task<Result<CurrenciesResponse>> Handle(GetCurrenciesQuery request, CancellationToken cancellationToken)
    {
        CurrencyOptions currencies = options.Value;
        string baseCurrency = currencies.Base.Trim().ToUpperInvariant();

        var rates = new SortedDictionary<string, decimal>(StringComparer.Ordinal) { [baseCurrency] = 1m };

        foreach ((string code, decimal rate) in currencies.Rates)
        {
            if (rate > 0)
            {
                rates[code.Trim().ToUpperInvariant()] = rate;
            }
        }

        rates[baseCurrency] = 1m;

        return Task.FromResult(Result.Success(new CurrenciesResponse(baseCurrency, rates, currencies.UpdatedOnUtc)));
    }
}
