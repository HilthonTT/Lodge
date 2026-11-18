using System.Net;
using System.Net.Http.Json;
using Lodge.Api.FunctionalTests.Abstractions;
using Lodge.Contracts.Apartments;
using Lodge.Contracts.Common;

namespace Lodge.Api.FunctionalTests.Apartments;

public class LocationSuggestTests(FunctionalTestWebAppFactory factory) : HostingFunctionalTest(factory)
{
    [Fact]
    public async Task Suggest_Should_MatchCitiesCaseInsensitively()
    {
        List<LocationSuggestionResponse>? suggestions =
            await HttpClient.GetFromJsonAsync<List<LocationSuggestionResponse>>("api/v1/locations/suggest?q=ROVA");

        suggestions.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Label = "Rovaniemi, Finland",
            City = "Rovaniemi",
            Country = "Finland",
            ListingCount = 1,
        });
        suggestions![0].Latitude.Should().BeApproximately(66.5, 0.1);
    }

    [Fact]
    public async Task Suggest_Should_MatchCountriesAndStates_AndGroupListings()
    {
        List<LocationSuggestionResponse>? california =
            await HttpClient.GetFromJsonAsync<List<LocationSuggestionResponse>>("api/v1/locations/suggest?q=californ");
        california!.Select(s => s.City).Should().BeEquivalentTo("Malibu", "Guerneville");

        List<LocationSuggestionResponse>? unitedStates =
            await HttpClient.GetFromJsonAsync<List<LocationSuggestionResponse>>("api/v1/locations/suggest?q=united");
        unitedStates!.Count.Should().BeLessThanOrEqualTo(8);
        unitedStates.Select(s => s.Label).Should().Contain(["Aspen, United States", "Bibury, United Kingdom"]);
    }

    [Fact]
    public async Task Suggest_Should_IgnoreAccents()
    {
        (HttpClient host, _) = await CreateAuthenticatedUserAsync();
        await CreateListingAsync(host, listing => listing with
        {
            Address = listing.Address with { City = "Zürich", Country = "Switzerland", State = "Zürich" },
        });

        List<LocationSuggestionResponse>? suggestions =
            await HttpClient.GetFromJsonAsync<List<LocationSuggestionResponse>>("api/v1/locations/suggest?q=zurich");

        suggestions.Should().ContainSingle().Which.Label.Should().Be("Zürich, Switzerland");

        // The search box term is accent-insensitive too.
        PagedList<ApartmentResponse>? search = await HttpClient.GetFromJsonAsync<PagedList<ApartmentResponse>>(
            "api/v1/apartments/search?searchTerm=zurich");
        search!.Items.Should().ContainSingle().Which.Address.City.Should().Be("Zürich");

        (await HttpClient.GetFromJsonAsync<PagedList<ApartmentResponse>>("api/v1/apartments/search?searchTerm=chateau"))!
            .Items.Select(a => a.Name).Should().Contain("Château des Vignes");
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("%20b%20")]
    public async Task Suggest_Should_RequireAtLeastTwoCharacters(string q)
    {
        await ShouldFailWithAsync(
            await HttpClient.GetAsync($"api/v1/locations/suggest?q={q}"),
            HttpStatusCode.BadRequest,
            "Locations.InvalidQuery");
    }

    [Fact]
    public async Task Suggest_Should_TreatWildcardsLiterally()
    {
        (await HttpClient.GetFromJsonAsync<List<LocationSuggestionResponse>>("api/v1/locations/suggest?q=%25%25"))!
            .Should().BeEmpty();
    }
}
