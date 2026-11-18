using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Lodge.Api.FunctionalTests.Extensions;
using Lodge.Contracts.Bookings;
using Lodge.Contracts.Common;
using Lodge.Contracts.Hosting;
using Lodge.Domain.Bookings;
using Lodge.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lodge.Api.FunctionalTests.Abstractions;

/// <summary>
/// Helpers for tests that create listings. Test listings are placed in the South Atlantic (latitude around -40) so
/// that map-bounds searches can isolate them from the seeded ones, and their names start with "Zz" so that they sort
/// after the seeded listings.
/// </summary>
public abstract class HostingFunctionalTest(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    /// <summary>
    /// The smallest valid PNG header; the API only checks the leading bytes.
    /// </summary>
    protected static readonly byte[] PngBytes =
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52];

    private static int _dayOffset = 400;

    /// <summary>
    /// Returns a check-in date far enough in the future, unique per call, so tests never collide.
    /// </summary>
    protected static DateOnly NextFreeDate(int span = 40)
    {
        int offset = Interlocked.Add(ref _dayOffset, span);

        return DateOnly.FromDateTime(DateTime.UtcNow).AddDays(offset);
    }

    protected static async Task<HttpResponseMessage> UploadPhotoAsync(HttpClient client, byte[]? content = null)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content ?? PngBytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "file", "photo.png");

        return await client.PostAsync("api/v1/hosting/photos", form);
    }

    protected static async Task<Guid> UploadPhotoIdAsync(HttpClient client)
    {
        HttpResponseMessage response = await UploadPhotoAsync(client);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<PhotoUploadResponse>())!.FileId;
    }

    protected static ListingRequest NewListing(Guid photoId, double latitude = -40, double longitude = -20) => new()
    {
        Name = $"Zz Test listing {Guid.NewGuid():N}",
        Description = "A quiet test flat with a view of the test harbour.",
        PropertyType = "Apartment",
        Address = new ListingAddressRequest
        {
            Country = "Testland",
            State = "Test State",
            ZipCode = "0000",
            City = "Testville",
            Street = "1 Test Street",
        },
        Latitude = latitude,
        Longitude = longitude,
        Price = 100,
        Currency = "USD",
        CleaningFee = 20,
        Bedrooms = 1,
        Beds = 1,
        Bathrooms = 1,
        MaximumGuestCount = 2,
        Amenities = ["WiFi"],
        CheckInInstructions = "The key is under the mat.",
        HouseRules = "No loud music.",
        InstantBook = true,
        MinNights = 1,
        MaxNights = 30,
        ImageIds = [photoId],
    };

    /// <summary>
    /// Registers a verified user (the host) and creates a listing for them.
    /// </summary>
    protected async Task<(HttpClient Client, Guid HostId, Guid ListingId)> CreateHostWithListingAsync(
        Func<ListingRequest, ListingRequest>? customize = null)
    {
        (HttpClient host, Guid hostId) = await CreateAuthenticatedUserAsync();

        Guid listingId = await CreateListingAsync(host, customize);

        return (host, hostId, listingId);
    }

    protected static async Task<Guid> CreateListingAsync(HttpClient host, Func<ListingRequest, ListingRequest>? customize = null)
    {
        ListingRequest request = NewListing(await UploadPhotoIdAsync(host));
        request = customize?.Invoke(request) ?? request;

        HttpResponseMessage response = await host.PostAsJsonAsync("api/v1/hosting/listings", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;
    }

    /// <summary>
    /// Books the listing as a new verified guest and returns the guest and the booking id.
    /// </summary>
    protected async Task<(HttpClient Client, Guid GuestId, Guid BookingId)> BookAsGuestAsync(
        Guid listingId,
        DateOnly start,
        int nights,
        string? messageToHost = null)
    {
        (HttpClient guest, Guid guestId) = await CreateAuthenticatedUserAsync();

        HttpResponseMessage response = await PostBookingAsync(guest, guestId, listingId, start, nights, 1, messageToHost);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        return (guest, guestId, await response.Content.ReadFromJsonAsync<Guid>());
    }

    protected static async Task<BookingResponse> GetBookingAsync(HttpClient client, Guid bookingId) =>
        (await client.GetFromJsonAsync<BookingResponse>($"api/v1/bookings/{bookingId}"))!;

    protected static async Task ShouldFailWithAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        response.StatusCode.Should().Be(status, await response.Content.ReadAsStringAsync());

        var problem = await response.GetProblemDetailsAsync();

        if (problem.Title == "Validation.General")
        {
            problem.Errors.Select(error => error.Code).Should().Contain(code);
        }
        else
        {
            problem.Title.Should().Be(code);
        }
    }

    /// <summary>
    /// Changes a booking directly in the database (e.g. to simulate a Stripe payment or an old request).
    /// </summary>
    protected async Task UpdateBookingAsync(Guid bookingId, Action<Booking> update)
    {
        await using AsyncServiceScope scope = Factory.Services.CreateAsyncScope();
        LodgeDbContext dbContext = scope.ServiceProvider.GetRequiredService<LodgeDbContext>();

        Booking booking = await dbContext.Bookings.SingleAsync(b => b.Id == bookingId);
        update(booking);
        booking.ClearDomainEvents();

        await dbContext.SaveChangesAsync();
    }

    protected async Task ExecuteSqlAsync(FormattableString sql)
    {
        await using AsyncServiceScope scope = Factory.Services.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<LodgeDbContext>().Database.ExecuteSqlAsync(sql);
    }
}
