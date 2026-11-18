using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lodge.Api.FunctionalTests.Abstractions;
using Lodge.Contracts.Apartments;
using Lodge.Contracts.Common;
using Lodge.Contracts.Hosting;

namespace Lodge.Api.FunctionalTests.Hosting;

public class HostListingTests(FunctionalTestWebAppFactory factory) : HostingFunctionalTest(factory)
{
    [Fact]
    public async Task Hosting_Should_RequireAuthentication_AndAVerifiedEmail()
    {
        (await HttpClient.GetAsync("api/v1/hosting/listings")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await HttpClient.GetAsync("api/v1/hosting/summary")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        (HttpClient unverified, _) = await CreateAuthenticatedUserAsync(verifyEmail: false);

        await ShouldFailWithAsync(
            await unverified.PostAsJsonAsync("api/v1/hosting/listings", NewListing(Guid.NewGuid())),
            HttpStatusCode.Forbidden,
            "User.EmailNotVerified");
        await ShouldFailWithAsync(
            await unverified.GetAsync("api/v1/hosting/summary"),
            HttpStatusCode.Forbidden,
            "User.EmailNotVerified");
        await ShouldFailWithAsync(await UploadPhotoAsync(unverified), HttpStatusCode.Forbidden, "User.EmailNotVerified");
    }

    [Fact]
    public async Task Host_Should_CreateReadUpdateUnlistAndDeleteAListing()
    {
        (HttpClient host, Guid hostId) = await CreateAuthenticatedUserAsync();

        HttpResponseMessage upload = await UploadPhotoAsync(host);
        upload.StatusCode.Should().Be(HttpStatusCode.OK);
        PhotoUploadResponse photo = (await upload.Content.ReadFromJsonAsync<PhotoUploadResponse>())!;
        photo.Url.Should().Be($"{FunctionalTestWebAppFactory.PublicBaseUrl}/api/v1/files/{photo.FileId}");

        ListingRequest request = NewListing(photo.FileId) with
        {
            PropertyType = "cabin",
            Amenities = ["wifi", "HotTub", "Kitchen"],
            WeekendPrice = 130,
            WeeklyDiscountPercent = 10,
            MonthlyDiscountPercent = 20,
            Bathrooms = 1.5m,
            CheckInTime = "16:00",
            CheckOutTime = "10:00",
            PetsAllowed = true,
            InstantBook = false,
            MinNights = 2,
        };

        HttpResponseMessage created = await host.PostAsJsonAsync("api/v1/hosting/listings", request);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        Guid listingId = (await created.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;
        created.Headers.Location!.ToString().Should().EndWith($"/api/v1/hosting/listings/{listingId}");

        HostListingResponse? own = await host.GetFromJsonAsync<HostListingResponse>($"api/v1/hosting/listings/{listingId}");
        own!.CheckInInstructions.Should().Be("The key is under the mat.");
        own.IsListed.Should().BeTrue();
        own.PropertyType.Should().Be("Cabin");
        own.Amenities.Should().BeEquivalentTo("WiFi", "HotTub", "Kitchen");
        own.Images.Should().ContainSingle().Which.Should().Be(new ApartmentImageResponse(photo.FileId, photo.Url));
        own.ImageUrl.Should().Be(photo.Url);
        own.Host.Id.Should().Be(hostId);
        own.Host.FirstName.Should().Be("First");
        own.CheckInTime.Should().Be("16:00");
        own.Bathrooms.Should().Be(1.5m);
        own.WeekendPrice.Should().Be(130);
        own.MinNights.Should().Be(2);
        own.InstantBook.Should().BeFalse();
        own.Address.ZipCode.Should().Be("0000");

        // The public view has no private fields.
        HttpResponseMessage publicResponse = await HttpClient.GetAsync($"api/v1/apartments/{listingId}");
        publicResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using (JsonDocument document = JsonDocument.Parse(await publicResponse.Content.ReadAsStringAsync()))
        {
            document.RootElement.TryGetProperty("checkInInstructions", out _).Should().BeFalse();
            document.RootElement.TryGetProperty("isListed", out _).Should().BeFalse();
            document.RootElement.GetProperty("host").GetProperty("isSuperhost").GetBoolean().Should().BeFalse();
            document.RootElement.GetProperty("latitude").GetDouble().Should().Be(-40);
        }

        // Update: new name, a second photo first, the old photo second.
        Guid secondPhoto = await UploadPhotoIdAsync(host);
        HttpResponseMessage updated = await host.PutAsJsonAsync(
            $"api/v1/hosting/listings/{listingId}",
            request with { Name = request.Name + " (renovated)", ImageIds = [secondPhoto, photo.FileId] });
        updated.StatusCode.Should().Be(HttpStatusCode.NoContent);

        ApartmentResponse? afterUpdate = await HttpClient.GetFromJsonAsync<ApartmentResponse>($"api/v1/apartments/{listingId}");
        afterUpdate!.Name.Should().EndWith("(renovated)");
        afterUpdate.Images.Select(image => image.Id).Should().Equal(secondPhoto, photo.FileId);
        afterUpdate.ImageUrl.Should().EndWith(secondPhoto.ToString());

        PagedList<HostListingSummaryResponse>? mine =
            await host.GetFromJsonAsync<PagedList<HostListingSummaryResponse>>("api/v1/hosting/listings");
        mine!.Items.Should().ContainSingle(l => l.Id == listingId)
            .Which.Should().BeEquivalentTo(new { IsListed = true, InstantBook = false, PropertyType = "Cabin", ImageUrl = afterUpdate.ImageUrl });

        // Unlisted: hidden from guests (404), still visible to the host.
        (await host.PutAsJsonAsync($"api/v1/hosting/listings/{listingId}/status", new UpdateListingStatusRequest(false)))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await HttpClient.GetAsync($"api/v1/apartments/{listingId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await HttpClient.GetAsync($"api/v1/apartments/{listingId}/calendar")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await host.GetFromJsonAsync<HostListingResponse>($"api/v1/apartments/{listingId}"))!.IsListed.Should().BeFalse();
        (HttpClient stranger, _) = await CreateAuthenticatedUserAsync();
        (await stranger.GetAsync($"api/v1/apartments/{listingId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        PagedList<ApartmentResponse>? search = await HttpClient.GetFromJsonAsync<PagedList<ApartmentResponse>>(
            $"api/v1/apartments/search?searchTerm={Uri.EscapeDataString(request.Name)}");
        search!.Items.Should().BeEmpty();

        // Deleted: gone for everyone.
        (await host.DeleteAsync($"api/v1/hosting/listings/{listingId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await host.GetAsync($"api/v1/hosting/listings/{listingId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await host.GetAsync($"api/v1/apartments/{listingId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await host.GetFromJsonAsync<PagedList<HostListingSummaryResponse>>("api/v1/hosting/listings"))!
            .Items.Should().NotContain(l => l.Id == listingId);
    }

    [Fact]
    public async Task OtherHosts_Should_GetNotOwner_ButAdminsMayManageEveryListing()
    {
        (HttpClient host, _, Guid listingId) = await CreateHostWithListingAsync();
        (HttpClient other, _) = await CreateAuthenticatedUserAsync();
        ListingRequest request = NewListing(await UploadPhotoIdAsync(other));

        await ShouldFailWithAsync(await other.GetAsync($"api/v1/hosting/listings/{listingId}"), HttpStatusCode.Forbidden, "Apartment.NotOwner");
        await ShouldFailWithAsync(await other.PutAsJsonAsync($"api/v1/hosting/listings/{listingId}", request), HttpStatusCode.Forbidden, "Apartment.NotOwner");
        await ShouldFailWithAsync(
            await other.PutAsJsonAsync($"api/v1/hosting/listings/{listingId}/status", new UpdateListingStatusRequest(false)),
            HttpStatusCode.Forbidden,
            "Apartment.NotOwner");
        await ShouldFailWithAsync(await other.DeleteAsync($"api/v1/hosting/listings/{listingId}"), HttpStatusCode.Forbidden, "Apartment.NotOwner");
        await ShouldFailWithAsync(await other.GetAsync($"api/v1/hosting/listings/{listingId}/calendar"), HttpStatusCode.Forbidden, "Apartment.NotOwner");
        await ShouldFailWithAsync(
            await other.PutAsJsonAsync(
                $"api/v1/hosting/listings/{listingId}/calendar",
                new UpdateCalendarRequest(NextFreeDate(), NextFreeDate().AddDays(1), Blocked: true)),
            HttpStatusCode.Forbidden,
            "Apartment.NotOwner");

        // A photo uploaded by someone else cannot be attached.
        await ShouldFailWithAsync(
            await host.PutAsJsonAsync($"api/v1/hosting/listings/{listingId}", request),
            HttpStatusCode.BadRequest,
            "Apartment.InvalidImage");

        (HttpClient admin, _) = await LoginAsAdminAsync();
        (await admin.GetAsync($"api/v1/hosting/listings/{listingId}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await admin.PutAsJsonAsync($"api/v1/hosting/listings/{listingId}/status", new UpdateListingStatusRequest(true)))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await HttpClient.GetAsync($"api/v1/hosting/listings/{Guid.NewGuid()}")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await ShouldFailWithAsync(await host.GetAsync($"api/v1/hosting/listings/{Guid.NewGuid()}"), HttpStatusCode.NotFound, "Apartment.NotFound");
    }

    [Fact]
    public async Task Delete_Should_Conflict_WhileTheListingHasUpcomingBookings()
    {
        (HttpClient host, _, Guid listingId) = await CreateHostWithListingAsync();
        (HttpClient guest, _, Guid bookingId) = await BookAsGuestAsync(listingId, NextFreeDate(), 2);

        await ShouldFailWithAsync(
            await host.DeleteAsync($"api/v1/hosting/listings/{listingId}"),
            HttpStatusCode.Conflict,
            "Apartment.HasUpcomingBookings");

        (await guest.PutAsJsonAsync($"api/v1/bookings/{bookingId}/cancel", new { userId = (await GetBookingAsync(guest, bookingId)).UserId }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await host.DeleteAsync($"api/v1/hosting/listings/{listingId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Photos_Should_BeCheckedByTheirBytes()
    {
        (HttpClient host, _) = await CreateAuthenticatedUserAsync();

        await ShouldFailWithAsync(
            await UploadPhotoAsync(host, "this is not an image"u8.ToArray()),
            HttpStatusCode.BadRequest,
            "Apartment.InvalidPhoto");

        byte[] webp = [.. "RIFF"u8.ToArray(), 0, 0, 0, 0, .. "WEBP"u8.ToArray(), 0, 0, 0, 0];
        (await UploadPhotoAsync(host, webp)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_Should_ValidateTheListing()
    {
        (HttpClient host, _) = await CreateAuthenticatedUserAsync();
        ListingRequest request = NewListing(await UploadPhotoIdAsync(host));

        await ShouldFailWithAsync(
            await host.PostAsJsonAsync("api/v1/hosting/listings", request with { PropertyType = "Castle" }),
            HttpStatusCode.BadRequest,
            "Listing.InvalidPropertyType");
        await ShouldFailWithAsync(
            await host.PostAsJsonAsync("api/v1/hosting/listings", request with { ImageIds = [] }),
            HttpStatusCode.BadRequest,
            "Apartment.InvalidImageCount");
        await ShouldFailWithAsync(
            await host.PostAsJsonAsync("api/v1/hosting/listings", request with { HouseRules = new string('x', 2001) }),
            HttpStatusCode.BadRequest,
            "Apartment.HouseRulesTooLong");
        await ShouldFailWithAsync(
            await host.PostAsJsonAsync("api/v1/hosting/listings", request with { MonthlyDiscountPercent = 95 }),
            HttpStatusCode.BadRequest,
            "Apartment.InvalidDiscount");
    }

    [Fact]
    public async Task Update_Should_InvalidateTheCachedApartment()
    {
        (HttpClient host, _, Guid listingId) = await CreateHostWithListingAsync();
        HostListingResponse listing = (await host.GetFromJsonAsync<HostListingResponse>($"api/v1/hosting/listings/{listingId}"))!;

        // Cache the public apartment.
        (await HttpClient.GetFromJsonAsync<ApartmentResponse>($"api/v1/apartments/{listingId}"))!.Price.Should().Be(100);

        ListingRequest request = NewListing(listing.Images[0].Id) with { Name = listing.Name, Price = 175 };
        (await host.PutAsJsonAsync($"api/v1/hosting/listings/{listingId}", request)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await HttpClient.GetFromJsonAsync<ApartmentResponse>($"api/v1/apartments/{listingId}"))!.Price.Should().Be(175);
    }
}
