using System.Data;
using Lodge.Application.Abstractions.App;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Abstractions.Messaging;
using Lodge.Application.Core.Extensions;
using Lodge.Application.Payments;
using Lodge.Contracts.Common;
using Lodge.Contracts.Experiences;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Experiences;
using Microsoft.Extensions.Options;

namespace Lodge.Application.Experiences;

internal sealed class SearchExperiencesQueryHandler(
    IDbConnectionFactory factory,
    CurrencyConverter currencyConverter,
    IDateTimeProvider dateTimeProvider,
    IOptions<AppOptions> appOptions) : IQueryHandler<SearchExperiencesQuery, PagedList<ExperienceSummaryResponse>>
{
    public async Task<Result<PagedList<ExperienceSummaryResponse>>> Handle(
        SearchExperiencesQuery request,
        CancellationToken cancellationToken)
    {
        SearchExperiencesQuery query = request with
        {
            Category = string.IsNullOrWhiteSpace(request.Category)
                ? null
                : EnumNames.Parse<ExperienceCategory>(request.Category).ToString(),
        };

        using IDbConnection connection = await factory.GetOpenConnectionAsync(cancellationToken);

        (List<ExperienceSummaryResponse> items, int totalCount) = await ExperienceQueries.SearchAsync(
            connection,
            query,
            dateTimeProvider.UtcNow,
            currencyConverter.ToBaseSql("e.price_per_guest", "e.currency"),
            appOptions.Value);

        return new PagedList<ExperienceSummaryResponse>(items, request.Page, request.PageSize, totalCount);
    }
}
