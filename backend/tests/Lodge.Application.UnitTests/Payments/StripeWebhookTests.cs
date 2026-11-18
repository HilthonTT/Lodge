using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Lodge.Application.Abstractions.Data;
using Lodge.Application.Experiences;
using Lodge.Application.Payments;
using Lodge.Application.Stripe;
using Lodge.Application.Stripe.Webhook;
using Lodge.Domain.Bookings;
using Lodge.Domain.Core.Primitives;
using Lodge.Domain.Experiences;
using Lodge.Domain.Payouts;
using Lodge.Domain.Payouts.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Lodge.Application.UnitTests.Payments;

public class StripeWebhookTests
{
    private const string WebhookSecret = "whsec_test";
    private const string ConnectWebhookSecret = "whsec_connect";

    private readonly IPayoutRepository _payoutRepository = Substitute.For<IPayoutRepository>();
    private readonly StripeWebhookCommandHandler _handler;

    public StripeWebhookTests()
    {
        _handler = CreateHandler(new StripeOptions
        {
            SecretKey = "sk_test",
            WebhookSecret = WebhookSecret,
            ConnectWebhookSecret = ConnectWebhookSecret,
        });
    }

    private StripeWebhookCommandHandler CreateHandler(StripeOptions options) =>
        new(
            Substitute.For<IBookingRepository>(),
            _payoutRepository,
            new BookingPayments(
                Substitute.For<IDbContext>(),
                _payoutRepository,
                new BookingPricing(new PricingService(), Options.Create(new PaymentsOptions())),
                Substitute.For<IDateTimeProvider>()),
            Substitute.For<IExperienceRepository>(),
            new ExperiencePayments(
                Substitute.For<IDbContext>(),
                _payoutRepository,
                Options.Create(new PaymentsOptions()),
                Substitute.For<IDateTimeProvider>()),
            Options.Create(options),
            NullLogger<StripeWebhookCommandHandler>.Instance);

    [Fact]
    public async Task AccountUpdated_Should_ActivateThePayoutAccount()
    {
        Guid hostId = Guid.NewGuid();
        var account = PayoutAccount.Create(hostId, "acct_123");
        _payoutRepository.GetAccountByExternalIdAsync("acct_123", Arg.Any<CancellationToken>()).Returns(account);

        Result result = await _handler.Handle(Signed(AccountUpdated("acct_123", payoutsEnabled: true)), default);

        result.IsSuccess.Should().BeTrue();
        account.Status.Should().Be(PayoutAccountStatus.Active);
        account.PayoutsEnabled.Should().BeTrue();
        account.DomainEvents.Should().ContainSingle().Which.Should().Be(new PayoutAccountActivatedDomainEvent(hostId));
    }

    [Fact]
    public async Task AccountUpdated_Should_KeepPendingAccounts_AndIgnoreUnknownOnes()
    {
        var account = PayoutAccount.Create(Guid.NewGuid(), "acct_pending");
        _payoutRepository.GetAccountByExternalIdAsync("acct_pending", Arg.Any<CancellationToken>()).Returns(account);

        (await _handler.Handle(Signed(AccountUpdated("acct_pending", payoutsEnabled: false)), default)).IsSuccess.Should().BeTrue();
        (await _handler.Handle(Signed(AccountUpdated("acct_unknown", payoutsEnabled: true)), default)).IsSuccess.Should().BeTrue();

        account.Status.Should().Be(PayoutAccountStatus.Pending);
    }

    [Fact]
    public async Task Webhook_Should_AcceptEventsSignedWithTheConnectSecret()
    {
        // Connect events (account.updated) come from a separate Stripe endpoint with its own signing secret.
        var account = PayoutAccount.Create(Guid.NewGuid(), "acct_connect");
        _payoutRepository.GetAccountByExternalIdAsync("acct_connect", Arg.Any<CancellationToken>()).Returns(account);

        Result result = await _handler.Handle(
            Signed(AccountUpdated("acct_connect", payoutsEnabled: true), ConnectWebhookSecret),
            default);

        result.IsSuccess.Should().BeTrue();
        account.Status.Should().Be(PayoutAccountStatus.Active);
    }

    [Fact]
    public async Task Webhook_Should_WorkWithOnlyTheConnectSecret_AndRejectOtherSecrets()
    {
        StripeWebhookCommandHandler connectOnly = CreateHandler(new StripeOptions { ConnectWebhookSecret = ConnectWebhookSecret });

        (await connectOnly.Handle(Signed(AccountUpdated("acct_unknown", true), ConnectWebhookSecret), default))
            .IsSuccess.Should().BeTrue();
        (await connectOnly.Handle(Signed(AccountUpdated("acct_unknown", true), WebhookSecret), default))
            .Error.Code.Should().Be("Stripe.InvalidSignature");
        (await _handler.Handle(Signed(AccountUpdated("acct_unknown", true), "whsec_other"), default))
            .Error.Code.Should().Be("Stripe.InvalidSignature");
        (await CreateHandler(new StripeOptions()).Handle(Signed(AccountUpdated("acct_unknown", true)), default))
            .Error.Code.Should().Be("Stripe.NotConfigured");
    }

    [Fact]
    public async Task Webhook_Should_RejectInvalidSignatures()
    {
        Result result = await _handler.Handle(
            new StripeWebhookCommand(AccountUpdated("acct_1", true), "t=1,v1=bad"),
            default);

        result.Error.Code.Should().Be("Stripe.InvalidSignature");
    }

    private static string AccountUpdated(string accountId, bool payoutsEnabled) =>
        $$"""
        {
          "id": "evt_test",
          "object": "event",
          "api_version": "2025-01-27.acacia",
          "created": 1700000000,
          "type": "account.updated",
          "data": {
            "object": {
              "id": "{{accountId}}",
              "object": "account",
              "payouts_enabled": {{(payoutsEnabled ? "true" : "false")}},
              "details_submitted": true
            }
          }
        }
        """;

    private static StripeWebhookCommand Signed(string payload, string secret = WebhookSecret)
    {
        string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        byte[] hash = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(secret),
            Encoding.UTF8.GetBytes($"{timestamp}.{payload}"));

        return new StripeWebhookCommand(payload, $"t={timestamp},v1={Convert.ToHexStringLower(hash)}");
    }
}
