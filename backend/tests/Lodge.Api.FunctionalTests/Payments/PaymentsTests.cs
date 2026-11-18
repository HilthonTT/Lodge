using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lodge.Api.FunctionalTests.Abstractions;
using Lodge.Application.Payouts;
using Lodge.Contracts.Bookings;
using Lodge.Contracts.Common;
using Lodge.Contracts.Emails;
using Lodge.Contracts.Hosting;
using Lodge.Contracts.Payments;
using Lodge.Contracts.Stripe;

namespace Lodge.Api.FunctionalTests.Payments;

/// <summary>
/// Simulated payments, refunds and payouts (the functional tests run in Development without Stripe keys).
/// Test listings are in "Testland" (no taxes) unless a country is set; the guest service fee is 12% and the host fee 3%.
/// </summary>
public class PaymentsTests(FunctionalTestWebAppFactory factory) : HostingFunctionalTest(factory)
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task Config_And_Currencies_Should_BeAnonymous()
    {
        PaymentsConfigResponse? config = await HttpClient.GetFromJsonAsync<PaymentsConfigResponse>("api/v1/payments/config");
        config!.Mode.Should().Be("Simulated");

        using JsonDocument document = JsonDocument.Parse(await HttpClient.GetStringAsync("api/v1/currencies"));
        document.RootElement.GetProperty("base").GetString().Should().Be("USD");
        document.RootElement.GetProperty("updatedOnUtc").GetDateTime().Should().BeBefore(DateTime.UtcNow);

        JsonElement rates = document.RootElement.GetProperty("rates");
        rates.EnumerateObject().Select(rate => rate.Name).Should().BeEquivalentTo(
            "USD", "EUR", "GBP", "JPY", "CHF", "CAD", "AUD");
        rates.GetProperty("USD").GetDecimal().Should().Be(1);
    }

    [Fact]
    public async Task SimulatedCheckout_Should_ConfirmTheBooking_AndScheduleThePayout()
    {
        (HttpClient host, _, Guid listingId) = await CreateHostWithListingAsync(listing => listing with
        {
            Address = listing.Address with { Country = "Netherlands" },
        });
        DateOnly start = NextFreeDate();
        (HttpClient guest, Guid guestId, Guid bookingId) = await BookAsGuestAsync(listingId, start, 3);

        // Nights 300 + WiFi 2% (6) + cleaning 20 = 326; service fee 12% = 39.12; tax 12.5% of 300 = 37.50.
        BookingResponse reserved = await GetBookingAsync(guest, bookingId);
        reserved.Should().BeEquivalentTo(new
        {
            ServiceFeeAmount = 39.12m,
            TaxAmount = 37.50m,
            TaxRatePercent = 12.5m,
            TotalPriceAmount = 402.62m,
            CancellationPolicy = "Moderate",
            AmountPaid = (decimal?)null,
            PaidOnUtc = (DateTime?)null,
            RefundStatus = "None",
        });

        HttpResponseMessage checkout = await guest.PostAsJsonAsync(
            "api/v1/stripe/checkout",
            new StripeCheckoutRequest(guestId, bookingId));
        checkout.StatusCode.Should().Be(HttpStatusCode.OK);
        (await checkout.Content.ReadFromJsonAsync<CheckoutResponse>())!.Url
            .Should().Be($"{FunctionalTestWebAppFactory.FrontendBaseUrl}/checkout/simulated?bookingId={bookingId}");

        (HttpClient stranger, _) = await CreateAuthenticatedUserAsync();
        await ShouldFailWithAsync(
            await stranger.PostAsync($"api/v1/payments/simulated/{bookingId}/complete", null),
            HttpStatusCode.Forbidden,
            "User.InvalidPermissions");

        (await guest.PostAsync($"api/v1/payments/simulated/{bookingId}/complete", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        // Replays are harmless, like Stripe webhook retries.
        (await guest.PostAsync($"api/v1/payments/simulated/{bookingId}/complete", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        BookingResponse confirmed = await GetBookingAsync(guest, bookingId);
        confirmed.Status.Should().Be(2);
        confirmed.AmountPaid.Should().Be(402.62m);
        confirmed.PaidOnUtc.Should().NotBeNull();

        // A paid booking can no longer go through the checkout.
        await ShouldFailWithAsync(
            await guest.PostAsJsonAsync("api/v1/stripe/checkout", new StripeCheckoutRequest(guestId, bookingId)),
            HttpStatusCode.BadRequest,
            "Stripe.BookingNotPayable");

        // The host gets the stay subtotal minus 3% (9.78): no service fee, no taxes.
        PagedList<PayoutResponse>? payouts = await host.GetFromJsonAsync<PagedList<PayoutResponse>>("api/v1/hosting/payouts");
        payouts!.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            BookingId = bookingId,
            Amount = 316.22m,
            Currency = "USD",
            Status = "Scheduled",
            ScheduledFor = start.AddDays(1),
            PaidOnUtc = (DateTime?)null,
            GuestName = "First Last",
        });

        HostingSummaryResponse? summary = await host.GetFromJsonAsync<HostingSummaryResponse>("api/v1/hosting/summary");
        summary!.UpcomingPayouts.Should().Be(316.22m);
        summary.PaidTotal.Should().Be(0);
    }

    [Fact]
    public async Task CancellationQuote_And_Cancel_Should_RefundThroughTheEventPipeline()
    {
        (HttpClient host, _, Guid listingId) = await CreateHostWithListingAsync(listing => listing with
        {
            Address = listing.Address with { Country = "France" },
            CancellationPolicy = "Strict",
            Amenities = [],
        });

        // Ten days before check-in: 50% under the Strict policy.
        DateOnly start = Today.AddDays(10);
        (HttpClient guest, Guid guestId, Guid bookingId) = await BookAsGuestAsync(listingId, start, 4);

        // Unpaid: nothing to refund.
        CancellationQuoteResponse? unpaid = await guest.GetFromJsonAsync<CancellationQuoteResponse>(
            $"api/v1/bookings/{bookingId}/cancellation-quote");
        unpaid!.Should().BeEquivalentTo(new
        {
            Policy = "Strict",
            RefundPercent = 50,
            RefundAmount = 0m,
            NonRefundableAmount = 0m,
            Currency = "USD",
            IsPaid = false,
        });

        await PayAsync(guest, guestId, bookingId);

        // Nights 400, cleaning 20, service fee 50.40, tax 5% = 20: total 490.40.
        // Refund: 50% of the nights (200) + 50% of the taxes (10) + the cleaning fee (20); the service fee is kept.
        CancellationQuoteResponse? quote = await guest.GetFromJsonAsync<CancellationQuoteResponse>(
            $"api/v1/bookings/{bookingId}/cancellation-quote");
        quote!.Should().BeEquivalentTo(new
        {
            Policy = "Strict",
            RefundPercent = 50,
            RefundAmount = 230m,
            NonRefundableAmount = 260.40m,
            Currency = "USD",
            IsPaid = true,
        });
        quote.Timeline.Should().Equal(
            new RefundTimelineEntryResponse(start.AddDays(-14), 100),
            new RefundTimelineEntryResponse(start.AddDays(-7), 50),
            new RefundTimelineEntryResponse(null, 0));

        await ShouldFailWithAsync(
            await host.GetAsync($"api/v1/bookings/{bookingId}/cancellation-quote"),
            HttpStatusCode.Forbidden,
            "User.InvalidPermissions");

        HttpResponseMessage cancelled = await guest.PutAsJsonAsync(
            $"api/v1/bookings/{bookingId}/cancel",
            new CancelBookingRequest(guestId));
        cancelled.StatusCode.Should().Be(HttpStatusCode.NoContent, await cancelled.Content.ReadAsStringAsync());

        BookingResponse pending = await GetBookingAsync(guest, bookingId);
        pending.Should().BeEquivalentTo(new { Status = 4, RefundAmount = 230m, RefundStatus = "Pending", CancelledByHost = false });

        // Outbox → integration event → refund processor (simulated) and the cancellation e-mail.
        await ProcessOutboxAsync();

        (await GetBookingAsync(guest, bookingId)).RefundStatus.Should().Be("Succeeded");
        Factory.Emails.Sent<BookingCancelledEmail>().Should().Contain(email =>
            email.RefundAmount == 230m && email.Currency == "USD" && !email.CancelledByHost && email.ListingName != null);

        // The host keeps the non-refunded nights (200) minus 3%.
        PagedList<PayoutResponse>? payouts = await host.GetFromJsonAsync<PagedList<PayoutResponse>>("api/v1/hosting/payouts");
        payouts!.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Amount = 194m, Status = "Scheduled" });
    }

    [Fact]
    public async Task HostCancellation_Should_RefundEverything_AndCountForTheSuperhostRate()
    {
        (HttpClient host, _, Guid listingId) = await CreateHostWithListingAsync(listing => listing with
        {
            CancellationPolicy = "Strict",
        });
        (HttpClient guest, Guid guestId, Guid bookingId) = await BookAsGuestAsync(listingId, NextFreeDate(), 2);

        // Only paid (confirmed) bookings can be cancelled by the host.
        await ShouldFailWithAsync(
            await host.PutAsync($"api/v1/hosting/bookings/{bookingId}/cancel", null),
            HttpStatusCode.BadRequest,
            "Booking.NotConfirmed");

        await PayAsync(guest, guestId, bookingId);

        (HttpClient otherHost, _) = await CreateAuthenticatedUserAsync();
        await ShouldFailWithAsync(
            await otherHost.PutAsync($"api/v1/hosting/bookings/{bookingId}/cancel", null),
            HttpStatusCode.Forbidden,
            "Apartment.NotOwner");

        (await host.PutAsync($"api/v1/hosting/bookings/{bookingId}/cancel", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        BookingResponse booking = await GetBookingAsync(guest, bookingId);
        booking.Status.Should().Be(4);
        booking.CancelledByHost.Should().BeTrue();
        booking.RefundAmount.Should().Be(booking.TotalPriceAmount);

        await ProcessOutboxAsync();

        (await GetBookingAsync(guest, bookingId)).RefundStatus.Should().Be("Succeeded");
        Factory.Emails.Sent<BookingCancelledEmail>().Should().Contain(email =>
            email.CancelledByHost && email.RefundAmount == booking.TotalPriceAmount);

        PagedList<PayoutResponse>? payouts = await host.GetFromJsonAsync<PagedList<PayoutResponse>>(
            "api/v1/hosting/payouts?status=Cancelled");
        payouts!.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(new { BookingId = bookingId, Amount = 0m });

        // One booking, cancelled by the host.
        HostingSummaryResponse? summary = await host.GetFromJsonAsync<HostingSummaryResponse>("api/v1/hosting/summary");
        summary!.Superhost.CancellationRate.Should().Be(1m);
    }

    [Fact]
    public async Task Payouts_Should_BeHeldUntilTheHostConnects_ThenPaid()
    {
        (HttpClient host, Guid hostId, Guid listingId) = await CreateHostWithListingAsync();
        (HttpClient guest, Guid guestId, Guid bookingId) = await BookAsGuestAsync(listingId, NextFreeDate(), 2);
        await PayAsync(guest, guestId, bookingId);

        // The stay has started: the payout is due.
        await ExecuteSqlAsync($"UPDATE payouts SET scheduled_for = CURRENT_DATE - 1 WHERE booking_id = {bookingId}");

        PayoutAccountResponse? account = await host.GetFromJsonAsync<PayoutAccountResponse>("api/v1/hosting/payouts/account");
        account.Should().Be(new PayoutAccountResponse("NotConnected", false, "Simulated"));

        await RunAsync<IPayDuePayoutsJob, int>(job => job.PayDueAsync());

        PagedList<PayoutResponse>? held = await host.GetFromJsonAsync<PagedList<PayoutResponse>>(
            "api/v1/hosting/payouts?status=OnHold");
        held!.Items.Should().ContainSingle().Which.BookingId.Should().Be(bookingId);

        await ShouldFailWithAsync(
            await host.GetAsync("api/v1/hosting/payouts?status=Lost"),
            HttpStatusCode.BadRequest,
            "Payouts.InvalidStatus");

        // Simulated onboarding activates the account immediately.
        HttpResponseMessage onboarding = await host.PostAsync("api/v1/hosting/payouts/onboarding", null);
        onboarding.StatusCode.Should().Be(HttpStatusCode.OK);
        (await onboarding.Content.ReadFromJsonAsync<UrlResponse>())!.Url
            .Should().Be($"{FunctionalTestWebAppFactory.FrontendBaseUrl}/hosting/payouts?onboarding=complete");

        (await host.GetFromJsonAsync<PayoutAccountResponse>("api/v1/hosting/payouts/account"))
            .Should().Be(new PayoutAccountResponse("Active", true, "Simulated"));

        // PayoutAccountActivated → outbox → the payouts on hold are paid.
        await ProcessOutboxAsync();

        PagedList<PayoutResponse>? paid = await host.GetFromJsonAsync<PagedList<PayoutResponse>>("api/v1/hosting/payouts");
        PayoutResponse payout = paid!.Items.Should().ContainSingle().Subject;
        payout.Status.Should().Be("Paid");
        payout.PaidOnUtc.Should().NotBeNull();

        HostingSummaryResponse? summary = await host.GetFromJsonAsync<HostingSummaryResponse>("api/v1/hosting/summary");
        summary!.PaidTotal.Should().Be(payout.Amount);
        summary.UpcomingPayouts.Should().Be(0);

        // The daily job pays due payouts of connected hosts right away.
        (_, _, Guid secondBookingId) = await BookAsGuestAsync(listingId, NextFreeDate(), 2);
        await ExecuteSqlAsync($"UPDATE bookings SET status = 2 WHERE id = {secondBookingId}");
        await ExecuteSqlAsync(
            $"""
            INSERT INTO payouts (id, booking_id, host_id, amount_amount, amount_currency, host_fee_percent, status,
                scheduled_for, created_on_utc)
            VALUES ({Guid.NewGuid()}, {secondBookingId}, {hostId}, 100, 'USD', 3, 'Scheduled', CURRENT_DATE, now())
            """);

        (await RunAsync<IPayDuePayoutsJob, int>(job => job.PayDueAsync())).Should().BeGreaterThanOrEqualTo(1);

        (await host.GetFromJsonAsync<PagedList<PayoutResponse>>("api/v1/hosting/payouts?status=Paid"))!
            .Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task SeededData_Should_IncludeRefunds_PayoutHistory_AndPayoutAccounts()
    {
        (HttpClient demo, Guid demoId) = await LoginAsDemoUserAsync();
        List<BookingResponse>? bookings = await demo.GetFromJsonAsync<List<BookingResponse>>($"api/v1/users/{demoId}/bookings");

        BookingResponse refunded = bookings!.Should().ContainSingle(b => b.RefundStatus == "Succeeded").Subject;
        refunded.Should().BeEquivalentTo(new { Status = 4, ApartmentName = "Aurora Glass Igloo", CancellationPolicy = "Strict" });
        refunded.RefundAmount.Should().BeGreaterThan(0).And.BeLessThan(refunded.TotalPriceAmount);
        refunded.AmountPaid.Should().Be(refunded.TotalPriceAmount);

        bookings.Should().ContainSingle(b => b.Status == 5 && b.CanReview && !b.HasReview)
            .Which.ApartmentName.Should().Be("Redwood Treehouse");
        bookings.Where(b => b.Status is 2 or 5).Should().OnlyContain(b => b.AmountPaid > 0 && b.PaidOnUtc != null);

        (HttpClient sam, _) = await LoginAsHostAsync();
        (await sam.GetFromJsonAsync<PayoutAccountResponse>("api/v1/hosting/payouts/account"))!
            .Should().BeEquivalentTo(new { Status = "Active", PayoutsEnabled = true });
        PagedList<PayoutResponse>? samPayouts = await sam.GetFromJsonAsync<PagedList<PayoutResponse>>(
            "api/v1/hosting/payouts?pageSize=50");
        samPayouts!.Items.Should().Contain(p => p.Status == "Paid" && p.PaidOnUtc != null);
        samPayouts.Items.Should().Contain(p => p.Status == "Scheduled");
        samPayouts.Items.Should().NotContain(p => p.Status == "OnHold");

        (HttpClient priya, _) = await LoginAsync("priya.patel@lodge.dev", DemoPassword);
        (await priya.GetFromJsonAsync<PayoutAccountResponse>("api/v1/hosting/payouts/account"))!
            .Status.Should().Be("NotConnected");
        PagedList<PayoutResponse>? priyaPayouts = await priya.GetFromJsonAsync<PagedList<PayoutResponse>>(
            "api/v1/hosting/payouts?status=OnHold");
        priyaPayouts!.Items.Should().NotBeEmpty();
    }
}
