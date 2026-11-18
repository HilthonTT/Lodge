using System.Net;
using System.Net.Http.Json;
using Lodge.Api.FunctionalTests.Abstractions;
using Lodge.Contracts.Apartments;
using Lodge.Contracts.Common;
using Lodge.Contracts.Hosting;

namespace Lodge.Api.FunctionalTests.Apartments;

public class SearchFilterTests(FunctionalTestWebAppFactory factory) : HostingFunctionalTest(factory)
{
    private async Task<List<ApartmentResponse>> SearchAsync(string queryString)
    {
        HttpResponseMessage response = await HttpClient.GetAsync($"api/v1/apartments/search?pageSize=100&{queryString}");
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        return [.. (await response.Content.ReadFromJsonAsync<PagedList<ApartmentResponse>>())!.Items];
    }

    private static IEnumerable<string> Names(IEnumerable<ApartmentResponse> apartments) => apartments.Select(a => a.Name);

    [Fact]
    public async Task Search_Should_FilterByPrice()
    {
        List<ApartmentResponse> results = await SearchAsync("minPrice=300&maxPrice=700");

        results.Should().NotBeEmpty().And.OnlyContain(a => a.Price >= 300 && a.Price <= 700);
        Names(results).Should().Contain(["Aurora Glass Igloo", "Hillside Luxury Villa", "Minimalist Lake House"]);
        Names(results).Should().NotContain(["Château des Vignes", "Pine Ridge Cabin"]);
    }

    [Fact]
    public async Task Search_Should_RequireAllAmenities()
    {
        List<ApartmentResponse> results = await SearchAsync("amenities=HotTub&amenities=Fireplace");

        Names(results).Should().Contain("Pine Ridge Cabin").And.NotContain("Hillside Luxury Villa");
        results.Should().OnlyContain(a => a.Amenities.Contains("HotTub") && a.Amenities.Contains("Fireplace"));

        // Comma-separated values work too.
        Names(await SearchAsync("amenities=HotTub,SwimmingPool")).Should().Contain("Hillside Luxury Villa").And.NotContain("Pine Ridge Cabin");
    }

    [Fact]
    public async Task Search_Should_MatchAnyOfThePropertyTypes()
    {
        List<ApartmentResponse> results = await SearchAsync("propertyTypes=Igloo&propertyTypes=treehouse");

        results.Should().OnlyContain(a => a.PropertyType == "Igloo" || a.PropertyType == "Treehouse");
        Names(results).Should().Contain(["Aurora Glass Igloo", "Redwood Treehouse"]);
    }

    [Fact]
    public async Task Search_Should_FilterByMinimumBedroomsBedsAndBathrooms()
    {
        Names(await SearchAsync("bedrooms=5")).Should().Contain(["Hillside Luxury Villa", "Château des Vignes"])
            .And.NotContain("Maple Grove Family Home");
        (await SearchAsync("beds=6")).Should().OnlyContain(a => a.Beds >= 6);
        Names(await SearchAsync("bathrooms=2.5")).Should().Contain("Maple Grove Family Home").And.NotContain("Seaside Family Retreat");
        (await SearchAsync("bathrooms=2.5")).Should().OnlyContain(a => a.Bathrooms >= 2.5m);
    }

    [Fact]
    public async Task Search_Should_FilterByInstantBookAndSuperhost()
    {
        List<ApartmentResponse> requestToBook = await SearchAsync("instantBook=false");
        Names(requestToBook).Should().Contain(["Lavender Hill Cottage", "Aurora Glass Igloo", "Château des Vignes"]);
        requestToBook.Should().OnlyContain(a => !a.InstantBook);

        List<ApartmentResponse> superhost = await SearchAsync("superhost=true");
        superhost.Should().OnlyContain(a => a.Host.IsSuperhost);
        superhost.Count(a => a.Host.FirstName == "Sam").Should().Be(8);
        Names(superhost).Should().NotContain(["Canal Houseboat", "Minimalist Lake House"]);
        Names(await SearchAsync("superhost=false")).Should().Contain(["Canal Houseboat", "Minimalist Lake House"]);
    }

    [Fact]
    public async Task Search_Should_FilterByMapBounds_AndReturnCoordinates()
    {
        // Western Europe.
        List<ApartmentResponse> europe = await SearchAsync("minLat=36&maxLat=60&minLng=-10&maxLng=10");

        Names(europe).Should().Contain(
            ["Lavender Hill Cottage", "Seaside Family Retreat", "Canal Houseboat", "Château des Vignes"]);
        Names(europe).Should().NotContain(["Aurora Glass Igloo", "Pine Ridge Cabin", "Minimalist Lake House"]);
        europe.Should().OnlyContain(a => a.Latitude >= 36 && a.Latitude <= 60 && a.Longitude >= -10 && a.Longitude <= 10);

        // A box across the antimeridian (New Zealand to the dateline and beyond).
        Names(await SearchAsync("minLat=-50&maxLat=-40&minLng=160&maxLng=-170")).Should().Contain("Minimalist Lake House");
    }

    [Fact]
    public async Task Search_Should_ExcludeListings_WhoseMinimumStayIsLongerThanTheDates()
    {
        DateOnly start = NextFreeDate();

        // One night: listings with a minimum of 2 or more nights are excluded.
        List<ApartmentResponse> oneNight = await SearchAsync($"startDate={start:yyyy-MM-dd}&endDate={start.AddDays(1):yyyy-MM-dd}");

        oneNight.Should().OnlyContain(a => a.MinNights <= 1);
        Names(oneNight).Should().Contain("Canal Houseboat").And.NotContain("Pine Ridge Cabin");
    }

    [Fact]
    public async Task Search_Should_FindFlexibleWindows()
    {
        (HttpClient host, _, Guid listingId) = await CreateHostWithListingAsync(listing => listing with
        {
            Latitude = -71.5,
            Longitude = -11.5,
            MaxNights = 60,
        });
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly monthStart = new DateOnly(today.Year, today.Month, 1).AddMonths(4);
        DateOnly monthEnd = monthStart.AddMonths(1).AddDays(-1);
        string month = $"{monthStart:yyyy-MM}";
        const string Box = "minLat=-72&maxLat=-71&minLng=-12&maxLng=-11";

        (await SearchAsync($"{Box}&flexible=month&months={month}")).Should().ContainSingle(a => a.Id == listingId);

        // Block the whole month except Friday to Sunday of the second weekend.
        DateOnly friday = monthStart.AddDays(7);
        while (friday.DayOfWeek != DayOfWeek.Friday)
        {
            friday = friday.AddDays(1);
        }

        await BlockAsync(host, listingId, monthStart, friday.AddDays(-1));
        await BlockAsync(host, listingId, friday.AddDays(2), monthEnd.AddDays(30));

        (await SearchAsync($"{Box}&flexible=weekend&months={month}")).Should().ContainSingle(a => a.Id == listingId);
        (await SearchAsync($"{Box}&flexible=week&months={month}")).Should().BeEmpty();
        (await SearchAsync($"{Box}&flexible=month&months={month}")).Should().BeEmpty();

        // A month in the past has no window at all.
        (await SearchAsync($"{Box}&flexible=weekend&months={today.AddMonths(-2):yyyy-MM}")).Should().BeEmpty();

        // Free a full week and look in two months.
        await host.PutAsJsonAsync(
            $"api/v1/hosting/listings/{listingId}/calendar",
            new UpdateCalendarRequest(monthStart.AddDays(14), monthStart.AddDays(20), Blocked: false));
        (await SearchAsync($"{Box}&flexible=week&months={today:yyyy-MM},{month}")).Should().ContainSingle(a => a.Id == listingId);
    }

    [Theory]
    [InlineData("flexible=week&months=2030-01&startDate=2030-01-01&endDate=2030-01-05", "SearchApartments.FlexibleWithExactDates")]
    [InlineData("flexible=week", "SearchApartments.InvalidMonths")]
    [InlineData("flexible=fortnight&months=2030-01", "SearchApartments.InvalidFlexible")]
    [InlineData("amenities=Moat", "SearchApartments.InvalidAmenity")]
    [InlineData("propertyTypes=Castle", "SearchApartments.InvalidPropertyType")]
    [InlineData("minPrice=500&maxPrice=100", "SearchApartments.InvalidPriceRange")]
    [InlineData("minLat=10&maxLat=5", "SearchApartments.InvalidBounds")]
    public async Task Search_Should_RejectInvalidFilters(string queryString, string code)
    {
        await ShouldFailWithAsync(
            await HttpClient.GetAsync($"api/v1/apartments/search?{queryString}"),
            HttpStatusCode.BadRequest,
            code);
    }

    [Fact]
    public async Task PriceRange_Should_SpanTheListedApartments()
    {
        PriceRangeResponse? range = await HttpClient.GetFromJsonAsync<PriceRangeResponse>("api/v1/apartments/price-range");

        range!.Currency.Should().Be("USD");
        range.Min.Should().BeLessThanOrEqualTo(140);
        range.Max.Should().BeGreaterThanOrEqualTo(950);
    }

    private static async Task BlockAsync(HttpClient host, Guid listingId, DateOnly from, DateOnly to)
    {
        HttpResponseMessage response = await host.PutAsJsonAsync(
            $"api/v1/hosting/listings/{listingId}/calendar",
            new UpdateCalendarRequest(from, to, Blocked: true));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
