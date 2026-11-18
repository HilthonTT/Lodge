using System.Net;
using System.Net.Http.Json;
using Lodge.Api.FunctionalTests.Abstractions;
using Lodge.Contracts.Common;
using Lodge.Contracts.Hosting;
using Lodge.Contracts.Wishlists;

namespace Lodge.Api.FunctionalTests.Wishlists;

public class WishlistTests(FunctionalTestWebAppFactory factory) : HostingFunctionalTest(factory)
{
    [Fact]
    public async Task DemoUser_Should_HaveTheSeededDreamTripsWishlist()
    {
        (HttpClient demo, _) = await LoginAsDemoUserAsync();

        List<WishlistSummaryResponse>? wishlists = await demo.GetFromJsonAsync<List<WishlistSummaryResponse>>("api/v1/wishlists");

        WishlistSummaryResponse dreamTrips = wishlists!.Should().ContainSingle(w => w.Name == "Dream trips").Subject;
        dreamTrips.ItemCount.Should().Be(3);
        dreamTrips.CoverImageUrl.Should().StartWith(FunctionalTestWebAppFactory.PublicBaseUrl + "/images/");

        WishlistResponse? details = await demo.GetFromJsonAsync<WishlistResponse>($"api/v1/wishlists/{dreamTrips.Id}");
        details!.Items.Select(a => a.Name).Should().BeEquivalentTo("Aurora Glass Igloo", "Hillside Luxury Villa", "Canal Houseboat");

        List<Guid>? saved = await demo.GetFromJsonAsync<List<Guid>>("api/v1/wishlists/saved-apartment-ids");
        saved.Should().BeEquivalentTo(details.Items.Select(a => a.Id));
    }

    [Fact]
    public async Task User_Should_ManageTheirWishlists()
    {
        (HttpClient client, _) = await CreateAuthenticatedUserAsync();
        Guid canal = await GetInstantBookApartmentIdAsync();
        Guid cabin = (await FindApartmentAsync("Pine Ridge Cabin")).Id;

        HttpResponseMessage created = await client.PostAsJsonAsync("api/v1/wishlists", new WishlistNameRequest("  Summer 2027  "));
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        Guid wishlistId = (await created.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;

        (await client.GetFromJsonAsync<List<WishlistSummaryResponse>>("api/v1/wishlists"))!
            .Should().ContainSingle().Which.Should().BeEquivalentTo(new { Name = "Summer 2027", ItemCount = 0, CoverImageUrl = (string?)null });

        // Adding and removing are idempotent.
        (await client.PutAsync($"api/v1/wishlists/{wishlistId}/items/{canal}", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.PutAsync($"api/v1/wishlists/{wishlistId}/items/{canal}", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.PutAsync($"api/v1/wishlists/{wishlistId}/items/{cabin}", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        WishlistResponse? wishlist = await client.GetFromJsonAsync<WishlistResponse>($"api/v1/wishlists/{wishlistId}");
        wishlist!.Items.Select(a => a.Id).Should().Equal(cabin, canal);
        (await client.GetFromJsonAsync<List<WishlistSummaryResponse>>("api/v1/wishlists"))![0]
            .Should().BeEquivalentTo(new { ItemCount = 2, CoverImageUrl = wishlist.Items[0].ImageUrl });

        (await client.DeleteAsync($"api/v1/wishlists/{wishlistId}/items/{canal}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.DeleteAsync($"api/v1/wishlists/{wishlistId}/items/{canal}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetFromJsonAsync<List<Guid>>("api/v1/wishlists/saved-apartment-ids")).Should().Equal(cabin);

        (await client.PutAsJsonAsync($"api/v1/wishlists/{wishlistId}", new WishlistNameRequest("Winter"))).StatusCode
            .Should().Be(HttpStatusCode.NoContent);
        (await client.GetFromJsonAsync<WishlistResponse>($"api/v1/wishlists/{wishlistId}"))!.Name.Should().Be("Winter");

        await ShouldFailWithAsync(
            await client.PutAsync($"api/v1/wishlists/{wishlistId}/items/{Guid.NewGuid()}", null),
            HttpStatusCode.NotFound,
            "Apartment.NotFound");
        await ShouldFailWithAsync(
            await client.PutAsJsonAsync($"api/v1/wishlists/{wishlistId}", new WishlistNameRequest(new string('x', 51))),
            HttpStatusCode.BadRequest,
            "Wishlist.NameTooLong");
        await ShouldFailWithAsync(
            await client.PostAsJsonAsync("api/v1/wishlists", new WishlistNameRequest(" ")),
            HttpStatusCode.BadRequest,
            "Wishlist.NameRequired");

        (await client.DeleteAsync($"api/v1/wishlists/{wishlistId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetAsync($"api/v1/wishlists/{wishlistId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.GetFromJsonAsync<List<Guid>>("api/v1/wishlists/saved-apartment-ids")).Should().BeEmpty();
    }

    [Fact]
    public async Task Wishlists_Should_BeOwnerOnly_AndRequireAuthentication()
    {
        (HttpClient demo, _) = await LoginAsDemoUserAsync();
        Guid dreamTrips = (await demo.GetFromJsonAsync<List<WishlistSummaryResponse>>("api/v1/wishlists"))!
            .Single(w => w.Name == "Dream trips").Id;

        (HttpClient other, _) = await CreateAuthenticatedUserAsync();
        await ShouldFailWithAsync(await other.GetAsync($"api/v1/wishlists/{dreamTrips}"), HttpStatusCode.NotFound, "Wishlist.NotFound");
        await ShouldFailWithAsync(
            await other.PutAsJsonAsync($"api/v1/wishlists/{dreamTrips}", new WishlistNameRequest("Mine now")),
            HttpStatusCode.NotFound,
            "Wishlist.NotFound");
        await ShouldFailWithAsync(await other.DeleteAsync($"api/v1/wishlists/{dreamTrips}"), HttpStatusCode.NotFound, "Wishlist.NotFound");

        (await HttpClient.GetAsync("api/v1/wishlists")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await HttpClient.GetAsync("api/v1/wishlists/saved-apartment-ids")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Wishlists_Should_BeLimitedTo50PerUser()
    {
        (HttpClient client, _) = await CreateAuthenticatedUserAsync();

        for (int index = 0; index < 50; index++)
        {
            (await client.PostAsJsonAsync("api/v1/wishlists", new WishlistNameRequest($"List {index}")))
                .StatusCode.Should().Be(HttpStatusCode.Created);
        }

        await ShouldFailWithAsync(
            await client.PostAsJsonAsync("api/v1/wishlists", new WishlistNameRequest("One too many")),
            HttpStatusCode.Conflict,
            "Wishlist.LimitReached");
    }

    [Fact]
    public async Task Wishlists_Should_HideUnlistedApartments()
    {
        (HttpClient host, _, Guid listingId) = await CreateHostWithListingAsync();
        (HttpClient guest, _) = await CreateAuthenticatedUserAsync();
        HttpResponseMessage created = await guest.PostAsJsonAsync("api/v1/wishlists", new WishlistNameRequest("Later"));
        Guid wishlistId = (await created.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;

        (await guest.PutAsync($"api/v1/wishlists/{wishlistId}/items/{listingId}", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await host.PutAsJsonAsync($"api/v1/hosting/listings/{listingId}/status", new UpdateListingStatusRequest(false)))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await guest.GetFromJsonAsync<WishlistResponse>($"api/v1/wishlists/{wishlistId}"))!.Items.Should().BeEmpty();
        (await guest.GetFromJsonAsync<List<WishlistSummaryResponse>>("api/v1/wishlists"))!.Single().ItemCount.Should().Be(0);
        await ShouldFailWithAsync(
            await guest.PutAsync($"api/v1/wishlists/{wishlistId}/items/{listingId}", null),
            HttpStatusCode.NotFound,
            "Apartment.NotFound");
    }
}
