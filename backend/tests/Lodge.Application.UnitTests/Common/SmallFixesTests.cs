using FluentValidation.Results;
using Lodge.Application.Abstractions.Assets;
using Lodge.Application.Apartments.CalculatePrice;
using Lodge.Application.Reviews.Create;
using Lodge.Contracts.Common;
using Microsoft.Extensions.Options;

namespace Lodge.Application.UnitTests.Common;

public class SmallFixesTests
{
    [Theory]
    [InlineData(1, 10, 25, true)]
    [InlineData(2, 10, 25, true)]
    [InlineData(3, 10, 25, false)]
    [InlineData(1, 10, 10, false)]
    public void PagedList_HasNextPage_Should_ReflectRemainingItems(int page, int pageSize, int total, bool expected)
    {
        var list = new PagedList<int>([], page, pageSize, total);

        list.HasNextPage.Should().Be(expected);
    }

    [Fact]
    public void PriceQuery_CacheKey_Should_DependOnTheDates()
    {
        Guid apartmentId = Guid.NewGuid();
        var first = new CalculateApartmentPriceQuery(apartmentId, new DateOnly(2030, 1, 1), new DateOnly(2030, 1, 3));
        var second = first with { EndDate = new DateOnly(2030, 1, 10) };

        first.CacheKey.Should().NotBe(second.CacheKey);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(0, false)]
    [InlineData(6, false)]
    public void CreateReviewValidator_Should_AcceptRatingsFromOneToFive(int rating, bool valid)
    {
        var command = new CreateReviewCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), rating, "Lovely stay");

        ValidationResult result = new CreateReviewCommandValidator().Validate(command);

        result.IsValid.Should().Be(valid);
    }

    [Theory]
    [InlineData("/images/cabin.png", "http://localhost:8000/images/cabin.png")]
    [InlineData("images/cabin.png", "http://localhost:8000/images/cabin.png")]
    [InlineData("https://images.unsplash.com/x.jpg", "https://images.unsplash.com/x.jpg")]
    public void AssetUrlResolver_Should_PrefixRelativePathsOnly(string input, string expected)
    {
        var resolver = new AssetUrlResolver(Options.Create(new AssetsOptions { PublicBaseUrl = "http://localhost:8000/" }));

        resolver.Resolve(input).Should().Be(expected);
    }
}
