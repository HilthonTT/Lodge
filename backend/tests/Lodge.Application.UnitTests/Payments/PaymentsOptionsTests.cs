using Lodge.Application.Payments;
using Lodge.Contracts.Payments;
using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lodge.Application.UnitTests.Payments;

public class PaymentsOptionsTests
{
    [Theory]
    [InlineData(null, false, true, PaymentMode.Simulated)]
    [InlineData("", false, true, PaymentMode.Simulated)]
    [InlineData(null, true, true, PaymentMode.Stripe)]
    // Outside Development, simulated payments must be enabled explicitly.
    [InlineData(null, false, false, PaymentMode.Stripe)]
    [InlineData("Simulated", false, false, PaymentMode.Simulated)]
    [InlineData("simulated", true, false, PaymentMode.Simulated)]
    [InlineData("Stripe", false, true, PaymentMode.Stripe)]
    public void Mode_Should_DefaultToSimulatedInDevelopmentWithoutStripeKeys(
        string? configured,
        bool stripeConfigured,
        bool isDevelopment,
        PaymentMode expected)
    {
        PaymentModeProvider.Resolve(configured, stripeConfigured, isDevelopment).Should().Be(expected);
    }

    [Theory]
    [InlineData("Netherlands", 12.5)]
    [InlineData("netherlands", 12.5)]
    [InlineData("France", 5)]
    [InlineData("United States", 10)]
    [InlineData("Portugal", 4)]
    [InlineData("Finland", 10)]
    [InlineData("New Zealand", 0)]
    [InlineData("United Kingdom", 0)]
    [InlineData("Testland", 0)]
    public void TaxRates_Should_HaveDocumentedDefaults(string country, decimal expected)
    {
        new PaymentsOptions().TaxRateFor(country).Should().Be(expected);
    }

    [Fact]
    public void Configuration_Should_ExtendTheDefaultTaxRates()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Payments:TaxRates:Belgium"] = "7",
                ["Payments:TaxRates:France"] = "6",
                ["Payments:DefaultTaxRatePercent"] = "1",
                ["Payments:GuestServiceFeePercent"] = "14",
            })
            .Build();

        PaymentsOptions options = new ServiceCollection()
            .AddSingleton(configuration)
            .AddOptions<PaymentsOptions>().BindConfiguration(PaymentsOptions.SectionName).Services
            .BuildServiceProvider()
            .GetRequiredService<IOptions<PaymentsOptions>>().Value;

        options.TaxRateFor("Belgium").Should().Be(7);
        options.TaxRateFor("France").Should().Be(6);
        options.TaxRateFor("Netherlands").Should().Be(12.5m);
        options.TaxRateFor("Testland").Should().Be(1);
        options.GuestServiceFeePercent.Should().Be(14);
        options.HostServiceFeePercent.Should().Be(3);
    }

    [Fact]
    public void BookingPricing_Should_UseTheTaxRateOfTheListingCountry()
    {
        var pricing = new BookingPricing(new PricingService(), Options.Create(new PaymentsOptions()));
        Apartment apartment = TestApartments.Create(price: 100);
        DateOnly start = new(2026, 6, 8);

        PricingDetails details = pricing.Calculate(apartment, DateRange.Create(start, start.AddDays(2)).Value);

        details.TaxRatePercent.Should().Be(new PaymentsOptions().TaxRateFor(apartment.Address.Country));
        details.ServiceFee.Amount.Should().Be(PricingService.Round(details.StaySubtotal.Amount * 0.12m));
        pricing.HostServiceFeePercent.Should().Be(3);
    }

    [Fact]
    public async Task Currencies_Should_ListTheStaticRatesAgainstUsd()
    {
        var handler = new GetCurrenciesQueryHandler(Options.Create(new CurrencyOptions()));

        CurrenciesResponse response = (await handler.Handle(new GetCurrenciesQuery(), default)).Value;

        response.Base.Should().Be("USD");
        response.Rates.Keys.Should().BeEquivalentTo("USD", "EUR", "GBP", "JPY", "CHF", "CAD", "AUD");
        response.Rates["USD"].Should().Be(1);
        response.Rates.Values.Should().OnlyContain(rate => rate > 0);
    }

    [Fact]
    public void MinorUnits_Should_BeCents()
    {
        StripePaymentGateway.ToMinorUnits(123.455m).Should().Be(12346);
        StripePaymentGateway.ToMinorUnits(0.1m).Should().Be(10);
    }
}
