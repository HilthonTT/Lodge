using Lodge.Application.Abstractions.Messaging;
using Lodge.Contracts.Apartments;

namespace Lodge.Application.Locations.Suggest;

/// <summary>
/// Suggests destinations (city + country) for the search box: a case- and accent-insensitive "contains" match on the
/// city, state or country of listed apartments, most listings first, at most <see cref="MaxResults"/>.
/// </summary>
/// <param name="Query">The text typed so far (at least 2 characters).</param>
public sealed record SuggestLocationsQuery(string? Query) : IQuery<List<LocationSuggestionResponse>>
{
    public const int MinLength = 2;

    public const int MaxLength = 100;

    public const int MaxResults = 8;
}
