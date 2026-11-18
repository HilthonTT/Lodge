using System.Net.Http.Json;
using System.Text.Json;
using Lodge.Api.FunctionalTests.Abstractions;
using Lodge.Contracts.Apartments;
using Lodge.Contracts.Bookings;
using Lodge.Contracts.Common;
using Lodge.Contracts.Reviews;

namespace Lodge.Api.FunctionalTests.Apartments;

/// <summary>
/// Uses the seeded reviews: Pine Ridge Cabin (5, 5), Canal Houseboat (4, 3).
/// </summary>
public class RatingTests(FunctionalTestWebAppFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task Search_Should_ReturnAverageRatingAndReviewCount()
    {
        ApartmentResponse canal = await FindAsync("Canal Houseboat");

        canal.AverageRating.Should().Be(3.5m);
        canal.ReviewCount.Should().Be(2);
    }

    [Fact]
    public async Task GetById_And_List_Should_ReturnAverageRatingAndReviewCount()
    {
        ApartmentResponse pineRidge = await FindAsync("Pine Ridge Cabin");

        ApartmentResponse? byId = await HttpClient.GetFromJsonAsync<ApartmentResponse>(
            $"api/v1/apartments/{pineRidge.Id}");
        byId!.AverageRating.Should().Be(5.0m);
        byId.ReviewCount.Should().Be(2);

        PagedList<ApartmentResponse>? list = await HttpClient.GetFromJsonAsync<PagedList<ApartmentResponse>>(
            "api/v1/apartments?pageSize=100");
        list!.Items.Should().ContainSingle(a => a.Id == pineRidge.Id)
            .Which.Should().BeEquivalentTo(new { AverageRating = 5.0m, ReviewCount = 2 });
        list.Items.Should().OnlyContain(a =>
            (a.ReviewCount == 0 && a.AverageRating == null) ||
            (a.ReviewCount > 0 && a.AverageRating >= 1 && a.AverageRating <= 5));
    }

    [Theory]
    [InlineData("desc")]
    [InlineData("asc")]
    public async Task Search_Should_SortByRating_WithUnratedApartmentsLast(string sortOrder)
    {
        PagedList<ApartmentResponse>? page = await HttpClient.GetFromJsonAsync<PagedList<ApartmentResponse>>(
            $"api/v1/apartments/search?sortColumn=rating&sortOrder={sortOrder}&pageSize=100");

        List<decimal?> ratings = page!.Items.Select(a => a.AverageRating).ToList();
        List<decimal> rated = ratings.TakeWhile(r => r.HasValue).Select(r => r!.Value).ToList();

        ratings.Skip(rated.Count).Should().OnlyContain(r => r == null);
        rated.Should().NotBeEmpty();

        if (sortOrder == "desc")
        {
            rated.Should().BeInDescendingOrder();
        }
        else
        {
            rated.Should().BeInAscendingOrder();
        }
    }

    [Fact]
    public async Task ApartmentJson_Should_UseTheContractFieldNames()
    {
        using JsonDocument document = JsonDocument.Parse(
            await HttpClient.GetStringAsync("api/v1/apartments/search?searchTerm=Canal"));
        JsonElement apartment = document.RootElement.GetProperty("items")[0];

        apartment.GetProperty("averageRating").GetDecimal().Should().Be(3.5m);
        apartment.GetProperty("reviewCount").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task Reviews_Should_IncludeTheAuthorName()
    {
        ApartmentResponse pineRidge = await FindAsync("Pine Ridge Cabin");

        PagedList<ReviewResponse>? reviews = await HttpClient.GetFromJsonAsync<PagedList<ReviewResponse>>(
            $"api/v1/apartments/{pineRidge.Id}/reviews");

        reviews!.Items.Select(r => r.AuthorName).Should().BeEquivalentTo(["Alex M.", "Jamie L."]);

        using JsonDocument document = JsonDocument.Parse(
            await HttpClient.GetStringAsync($"api/v1/apartments/{pineRidge.Id}/reviews"));
        document.RootElement.GetProperty("items")[0].GetProperty("authorName").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Bookings_Should_ReportWhetherTheyHaveAReview()
    {
        (HttpClient client, Guid userId) = await LoginAsDemoUserAsync();

        List<BookingResponse>? bookings = await client.GetFromJsonAsync<List<BookingResponse>>(
            $"api/v1/users/{userId}/bookings");

        bookings.Should().NotBeNull();
        bookings!.Should().OnlyContain(b => b.GuestName == "Alex Morgan" && b.GuestEmail == DemoEmail);

        List<BookingResponse> completed = bookings.Where(b => b.Status == 5).ToList();
        completed.Should().Contain(b => b.ApartmentName == "Pine Ridge Cabin" && b.HasReview);
        completed.Should().Contain(b => b.ApartmentName == "Redwood Treehouse" && !b.HasReview);
        bookings.Where(b => b.Status != 5).Should().OnlyContain(b => !b.HasReview);
    }

    private async Task<ApartmentResponse> FindAsync(string name)
    {
        PagedList<ApartmentResponse>? page = await HttpClient.GetFromJsonAsync<PagedList<ApartmentResponse>>(
            $"api/v1/apartments/search?searchTerm={Uri.EscapeDataString(name)}");

        return page!.Items.Should().ContainSingle(a => a.Name == name).Subject;
    }
}
