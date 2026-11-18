using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Lodge.Api.FunctionalTests.Abstractions;
using Lodge.Api.FunctionalTests.Extensions;
using Lodge.Contracts.Bookings;
using Lodge.Presentation.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace Lodge.Api.FunctionalTests.Requests;

/// <summary>
/// Binding failures are 400 problems with stable codes (not 500), and idempotency keys replay the first result.
/// </summary>
public class RequestBindingAndIdempotencyTests(FunctionalTestWebAppFactory factory) : HostingFunctionalTest(factory)
{
    [Theory]
    [InlineData(null)]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task MissingOrInvalidIdempotencyKey_Should_Return400(string? key)
    {
        (HttpClient guest, Guid guestId) = await CreateAuthenticatedUserAsync();
        Guid apartmentId = await GetInstantBookApartmentIdAsync();
        DateOnly start = NextFreeDate();

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/bookings")
        {
            Content = JsonContent.Create(new ReserveBookingRequest(apartmentId, guestId, start, start.AddDays(2), 1, null)),
        };

        if (key is not null)
        {
            request.Headers.TryAddWithoutValidation("X-Idempotency-Key", key);
        }

        HttpResponseMessage response = await guest.SendAsync(request);

        await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, "Request.InvalidIdempotencyKey");
    }

    [Fact]
    public async Task MissingIdempotencyKey_Should_Return400_OnEveryIdempotentEndpoint()
    {
        (HttpClient guest, Guid guestId) = await CreateAuthenticatedUserAsync();

        (string Path, HttpContent Content)[] endpoints =
        [
            ("api/v1/reviews", JsonContent.Create(new { userId = guestId, bookingId = Guid.NewGuid(), rating = 5, comment = "Nice" })),
            ("api/v1/experience-bookings", JsonContent.Create(new { sessionId = Guid.NewGuid(), guestCount = 1 })),
            ("api/v1/files", FileForm(guestId, PngBytes)),
        ];

        foreach ((string path, HttpContent content) in endpoints)
        {
            HttpResponseMessage response = await guest.PostAsync(path, content);

            await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, "Request.InvalidIdempotencyKey");
        }
    }

    [Fact]
    public async Task MalformedJson_Should_Return400InvalidBody()
    {
        (HttpClient guest, _) = await CreateAuthenticatedUserAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/bookings")
        {
            Content = new StringContent("{ \"apartmentId\": ", Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString());

        await ShouldBeProblemAsync(await guest.SendAsync(request), HttpStatusCode.BadRequest, "Request.InvalidBody");

        // A wrongly typed property is a JSON error too.
        HttpResponseMessage wrongType = await HttpClient.PostAsync(
            "api/v1/authentication/login",
            new StringContent("{ \"email\": 42, \"password\": [] }", Encoding.UTF8, "application/json"));

        await ShouldBeProblemAsync(wrongType, HttpStatusCode.BadRequest, "Request.InvalidBody");
    }

    [Fact]
    public async Task UnsupportedBodyMediaType_Should_Return415_NotAServerFailure()
    {
        // Rejected by endpoint matching (the endpoint only accepts JSON) before any binding happens.
        HttpResponseMessage response = await HttpClient.PostAsync(
            "api/v1/authentication/login",
            new StringContent("email=a", Encoding.UTF8, "text/plain"));

        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public void BindingFailures_Should_BeClassifiedByTheirSource()
    {
        RequestErrors.From(new BadHttpRequestException("Failed to read parameter \"LoginRequest request\" from the request body as JSON.", new JsonException()))
            .Should().Be(RequestErrors.InvalidBody);
        RequestErrors.From(new BadHttpRequestException("Required parameter \"LoginRequest request\" was not provided from body."))
            .Should().Be(RequestErrors.InvalidBody);
        RequestErrors.From(new BadHttpRequestException("Expected a supported JSON media type but got \"text/plain\".", StatusCodes.Status415UnsupportedMediaType))
            .Should().Be(RequestErrors.InvalidBody);
        RequestErrors.From(new BadHttpRequestException("Failed to bind parameter \"int guestCount\" from \"nobody\"."))
            .Should().Be(RequestErrors.InvalidParameter);
        RequestErrors.From(new BadHttpRequestException("Required parameter \"string platform\" was not provided from query string."))
            .Should().Be(RequestErrors.InvalidParameter);
    }

    [Theory]
    [InlineData("api/v1/apartments/search?guestCount=abc")]
    [InlineData("api/v1/apartments/search?startDate=tomorrow")]
    public async Task WronglyTypedQueryValue_Should_Return400InvalidParameter(string url)
    {
        HttpResponseMessage response = await HttpClient.GetAsync(url);

        await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, "Request.InvalidParameter");
    }

    [Fact]
    public async Task Booking_Should_ReplayTheFirstResult_ForTheSameKey()
    {
        (HttpClient guest, Guid guestId) = await CreateAuthenticatedUserAsync();
        Guid apartmentId = await GetInstantBookApartmentIdAsync();
        DateOnly start = NextFreeDate();
        var body = new ReserveBookingRequest(apartmentId, guestId, start, start.AddDays(2), 1, null);
        Guid key = Guid.NewGuid();

        HttpResponseMessage first = await PostWithKeyAsync(guest, "api/v1/bookings", JsonContent.Create(body), key);
        HttpResponseMessage replay = await PostWithKeyAsync(guest, "api/v1/bookings", JsonContent.Create(body), key);

        first.StatusCode.Should().Be(HttpStatusCode.OK, await first.Content.ReadAsStringAsync());
        replay.StatusCode.Should().Be(HttpStatusCode.OK, await replay.Content.ReadAsStringAsync());
        Guid bookingId = await first.Content.ReadFromJsonAsync<Guid>();
        (await replay.Content.ReadFromJsonAsync<Guid>()).Should().Be(bookingId);

        (await guest.GetFromJsonAsync<List<BookingResponse>>($"api/v1/users/{guestId}/bookings") ?? [])
            .Should().ContainSingle().Which.Id.Should().Be(bookingId);

        // The same key on another idempotent endpoint is a conflict.
        HttpResponseMessage reused = await PostWithKeyAsync(guest, "api/v1/files", FileForm(guestId, PngBytes), key);
        await ShouldBeProblemAsync(reused, HttpStatusCode.Conflict, "Request.KeyReused");
    }

    [Fact]
    public async Task FileUpload_Should_ReplayTheFirstResult_ForTheSameKey()
    {
        (HttpClient user, Guid userId) = await CreateAuthenticatedUserAsync();
        Guid key = Guid.NewGuid();

        HttpResponseMessage first = await PostWithKeyAsync(user, "api/v1/files", FileForm(userId, PngBytes), key);
        HttpResponseMessage replay = await PostWithKeyAsync(user, "api/v1/files", FileForm(userId, PngBytes), key);

        first.StatusCode.Should().Be(HttpStatusCode.OK, await first.Content.ReadAsStringAsync());
        replay.StatusCode.Should().Be(HttpStatusCode.OK);
        (await replay.Content.ReadFromJsonAsync<Guid>()).Should().Be(await first.Content.ReadFromJsonAsync<Guid>());
    }

    [Fact]
    public async Task FileUpload_Should_CheckTheLeadingBytes_AndStoreTheDetectedContentType()
    {
        (HttpClient user, Guid userId) = await CreateAuthenticatedUserAsync();

        // A text file declared as an image.
        HttpResponseMessage fake = await PostWithKeyAsync(
            user, "api/v1/files", FileForm(userId, Encoding.UTF8.GetBytes("<svg onload=alert(1)>"), "image/png"), Guid.NewGuid());
        await ShouldFailWithAsync(fake, HttpStatusCode.BadRequest, "UploadFile.InvalidContentType");

        // A declared content type outside JPEG/PNG/WebP.
        HttpResponseMessage svg = await PostWithKeyAsync(
            user, "api/v1/files", FileForm(userId, PngBytes, "image/svg+xml"), Guid.NewGuid());
        await ShouldFailWithAsync(svg, HttpStatusCode.BadRequest, "UploadFile.InvalidContentType");

        // A PNG declared as a JPEG is stored as a PNG.
        HttpResponseMessage png = await PostWithKeyAsync(
            user, "api/v1/files", FileForm(userId, PngBytes, "image/jpeg"), Guid.NewGuid());
        png.StatusCode.Should().Be(HttpStatusCode.OK, await png.Content.ReadAsStringAsync());
        Guid fileId = await png.Content.ReadFromJsonAsync<Guid>();

        HttpResponseMessage download = await HttpClient.GetAsync($"api/v1/files/{fileId}");
        download.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
    }

    private static MultipartFormDataContent FileForm(Guid userId, byte[] bytes, string contentType = "image/png")
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(new StringContent(userId.ToString()), "userId");
        form.Add(file, "file", "photo.png");

        return form;
    }

    private static async Task<HttpResponseMessage> PostWithKeyAsync(HttpClient client, string path, HttpContent content, Guid key)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = content };
        request.Headers.Add("X-Idempotency-Key", key.ToString());

        return await client.SendAsync(request);
    }

    private static async Task ShouldBeProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        response.StatusCode.Should().Be(status, await response.Content.ReadAsStringAsync());
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var problem = await response.GetProblemDetailsAsync();
        problem.Title.Should().Be(code);
        problem.Status.Should().Be((int)status);
    }
}
