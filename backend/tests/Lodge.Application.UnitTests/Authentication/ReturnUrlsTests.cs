using Lodge.Application.Authentication;

namespace Lodge.Application.UnitTests.Authentication;

public class ReturnUrlsTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/trips")]
    [InlineData("/apartments/123?startDate=2026-12-01#reviews")]
    public void IsLocal_Should_AcceptRelativePaths(string url)
    {
        ReturnUrls.IsLocal(url).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("trips")]
    [InlineData("//evil.example")]
    [InlineData("/\\evil.example")]
    [InlineData("/foo\\bar")]
    [InlineData("https://evil.example/")]
    [InlineData("javascript:alert(1)")]
    [InlineData("/foo\r\nLocation: https://evil.example")]
    public void IsLocal_Should_RejectEverythingElse(string? url)
    {
        ReturnUrls.IsLocal(url).Should().BeFalse();
    }
}
