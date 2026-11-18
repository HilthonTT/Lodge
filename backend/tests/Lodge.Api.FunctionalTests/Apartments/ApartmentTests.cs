using System.Net;
using System.Net.Http.Json;
using Lodge.Api.FunctionalTests.Abstractions;
using Lodge.Contracts.Apartments;
using Lodge.Contracts.Common;

namespace Lodge.Api.FunctionalTests.Apartments;

public class ApartmentTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task Get_Should_ReturnSeededApartments_WithAbsoluteImageUrls()
    {
        PagedList<ApartmentResponse>? page = await HttpClient.GetFromJsonAsync<PagedList<ApartmentResponse>>(
            "api/v1/apartments");

        page.Should().NotBeNull();
        page!.Page.Should().Be(1);
        page.TotalCount.Should().BeGreaterThanOrEqualTo(10);
        page.Items.Should().NotBeEmpty();
        page.Items.Should().OnlyContain(a =>
            a.ImageUrl.StartsWith(FunctionalTestWebAppFactory.PublicBaseUrl + "/images/", StringComparison.Ordinal) &&
            a.MaximumGuestCount > 0);
    }

    [Fact]
    public async Task Get_Should_ReportNextPage_WhenMoreItemsExist()
    {
        // Regression: HasNextPage used to be inverted.
        PagedList<ApartmentResponse>? page = await HttpClient.GetFromJsonAsync<PagedList<ApartmentResponse>>(
            "api/v1/apartments?page=1&pageSize=3");

        page!.Items.Should().HaveCount(3);
        page.HasNextPage.Should().BeTrue();
        page.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task Get_Should_ReturnBadRequest_ForInvalidPageSize()
    {
        HttpResponseMessage response = await HttpClient.GetAsync("api/v1/apartments?page=1&pageSize=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Images_Should_BeServedAsStaticFiles()
    {
        HttpResponseMessage response = await HttpClient.GetAsync("images/cabin.png");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
    }

    [Fact]
    public async Task Price_Should_DependOnTheRequestedPeriod()
    {
        // Regression: the price was cached per apartment, ignoring the requested dates.
        PagedList<ApartmentResponse>? page = await HttpClient.GetFromJsonAsync<PagedList<ApartmentResponse>>(
            "api/v1/apartments?pageSize=1");
        Guid apartmentId = page!.Items[0].Id;

        PriceDetailsResponse? twoNights = await HttpClient.GetFromJsonAsync<PriceDetailsResponse>(
            $"api/v1/apartments/{apartmentId}/price?startDate=2031-01-01&endDate=2031-01-03");
        PriceDetailsResponse? fiveNights = await HttpClient.GetFromJsonAsync<PriceDetailsResponse>(
            $"api/v1/apartments/{apartmentId}/price?startDate=2031-01-01&endDate=2031-01-06");

        fiveNights!.PricePerPeriod.Should().Be(twoNights!.PricePerPeriod / 2 * 5);
    }

    [Fact]
    public async Task GetById_Should_ReturnApartment_FromCacheOnSecondCall()
    {
        PagedList<ApartmentResponse>? page = await HttpClient.GetFromJsonAsync<PagedList<ApartmentResponse>>(
            "api/v1/apartments?pageSize=1");
        Guid apartmentId = page!.Items[0].Id;

        ApartmentResponse? first = await HttpClient.GetFromJsonAsync<ApartmentResponse>($"api/v1/apartments/{apartmentId}");
        ApartmentResponse? cached = await HttpClient.GetFromJsonAsync<ApartmentResponse>($"api/v1/apartments/{apartmentId}");

        cached.Should().BeEquivalentTo(first);
        cached!.Address.City.Should().NotBeNullOrEmpty();
    }
}
