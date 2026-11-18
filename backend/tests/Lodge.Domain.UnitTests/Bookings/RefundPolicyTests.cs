using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Bookings.Events;
using Lodge.Domain.Core.Primitives;

namespace Lodge.Domain.UnitTests.Bookings;

/// <summary>
/// A 4-night stay at 100 per night with a 50 cleaning fee, a 12% service fee and a 10% tax:
/// nights 400, subtotal 450, service fee 54, taxes 40, total 544.
/// </summary>
public class RefundPolicyTests
{
    private static readonly DateTime UtcNow = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = DateOnly.FromDateTime(UtcNow);
    private static readonly FeeRates Fees = new(12, 10);

    [Theory]
    [InlineData(CancellationPolicy.Flexible, 30, 100)]
    [InlineData(CancellationPolicy.Flexible, 1, 100)]
    [InlineData(CancellationPolicy.Flexible, 0, 0)]
    [InlineData(CancellationPolicy.Moderate, 30, 100)]
    [InlineData(CancellationPolicy.Moderate, 5, 100)]
    [InlineData(CancellationPolicy.Moderate, 4, 50)]
    [InlineData(CancellationPolicy.Moderate, 0, 50)]
    [InlineData(CancellationPolicy.Strict, 30, 100)]
    [InlineData(CancellationPolicy.Strict, 14, 100)]
    [InlineData(CancellationPolicy.Strict, 13, 50)]
    [InlineData(CancellationPolicy.Strict, 7, 50)]
    [InlineData(CancellationPolicy.Strict, 6, 0)]
    [InlineData(CancellationPolicy.Strict, 0, 0)]
    public void RefundPercent_Should_FollowThePolicyBoundaries(CancellationPolicy policy, int days, int expected)
    {
        RefundPolicy.RefundPercent(policy, days).Should().Be(expected);
    }

    [Theory]
    // 100%: nights 400 + taxes 40 + cleaning 50 + service fee 54.
    [InlineData(CancellationPolicy.Flexible, 1, 100, 544, 0, 0)]
    // 0%: only the cleaning fee.
    [InlineData(CancellationPolicy.Flexible, 0, 0, 50, 494, 400)]
    [InlineData(CancellationPolicy.Moderate, 5, 100, 544, 0, 0)]
    // 50%: nights 200 + taxes 20 + cleaning 50; the service fee is kept.
    [InlineData(CancellationPolicy.Moderate, 4, 50, 270, 274, 200)]
    [InlineData(CancellationPolicy.Strict, 14, 100, 544, 0, 0)]
    [InlineData(CancellationPolicy.Strict, 13, 50, 270, 274, 200)]
    [InlineData(CancellationPolicy.Strict, 7, 50, 270, 274, 200)]
    [InlineData(CancellationPolicy.Strict, 6, 0, 50, 494, 400)]
    public void Cancel_Should_RefundAccordingToThePolicy(
        CancellationPolicy policy,
        int daysBeforeCheckIn,
        int percent,
        decimal refund,
        decimal nonRefundable,
        decimal retained)
    {
        Booking booking = PaidBooking(policy, Today.AddDays(daysBeforeCheckIn));

        Result<RefundQuote> result = booking.Cancel(UtcNow);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(new
        {
            Policy = policy,
            RefundPercent = percent,
            IsPaid = true,
            RefundAmount = refund,
            NonRefundableAmount = nonRefundable,
            RetainedStayAmount = retained,
        });
        booking.Status.Should().Be(BookingStatus.Cancelled);
        booking.RefundAmount.Should().Be(refund);
        booking.RefundStatus.Should().Be(RefundStatus.Pending);
        booking.CancelledByHost.Should().BeFalse();
        booking.DomainEvents.Should().ContainSingle(e => e is BookingRefundRequestedDomainEvent);
    }

    [Fact]
    public void Taxes_Should_BeRefundedProportionally_AndRounded()
    {
        // 3 nights at 99.99 with a 12.5% tax: taxes 37.50 (299.97 * 12.5% = 37.49625).
        Apartment apartment = TestData.CreateApartment(TestData.Details(99.99m, 0) with
        {
            CancellationPolicy = CancellationPolicy.Moderate,
        });
        Booking booking = TestData.Reserve(apartment, Today.AddDays(2), 3, UtcNow, fees: new FeeRates(0, 12.5m));
        booking.ConfirmPayment("pi_test", booking.TotalPrice.Amount, UtcNow);

        RefundQuote quote = booking.Cancel(UtcNow).Value;

        booking.Taxes.Amount.Should().Be(37.50m);
        quote.RefundPercent.Should().Be(50);
        // Nights 299.97 * 50% = 149.985 → 149.99; taxes 37.50 * 50% = 18.75.
        quote.RefundAmount.Should().Be(149.99m + 18.75m);
        quote.NonRefundableAmount.Should().Be(booking.TotalPrice.Amount - quote.RefundAmount);
    }

    [Fact]
    public void Cancel_Should_HaveNothingToRefund_ForAnUnpaidBooking()
    {
        Apartment apartment = TestData.CreateApartment(TestData.Details(100, 50));
        Booking booking = TestData.Reserve(apartment, Today.AddDays(1), 4, UtcNow, fees: Fees);

        RefundQuote quote = booking.Cancel(UtcNow).Value;

        quote.IsPaid.Should().BeFalse();
        quote.RefundAmount.Should().Be(0);
        quote.NonRefundableAmount.Should().Be(0);
        booking.RefundAmount.Should().BeNull();
        booking.RefundStatus.Should().Be(RefundStatus.None);
        booking.DomainEvents.Should().NotContain(e => e is BookingRefundRequestedDomainEvent);
    }

    [Fact]
    public void Cancel_Should_MarkTheRefundManual_WhenPaidBeforePaymentTracking()
    {
        Booking booking = PaidBooking(CancellationPolicy.Moderate, Today.AddDays(10), recordPayment: false);

        booking.Cancel(UtcNow).IsSuccess.Should().BeTrue();

        booking.RefundAmount.Should().Be(544);
        booking.RefundStatus.Should().Be(RefundStatus.Manual);
        booking.DomainEvents.Should().NotContain(e => e is BookingRefundRequestedDomainEvent);
    }

    [Fact]
    public void Cancel_Should_NotRequestARefund_WhenNothingIsRefunded()
    {
        Apartment apartment = TestData.CreateApartment(TestData.Details(100, 0) with
        {
            CancellationPolicy = CancellationPolicy.Strict,
        });
        Booking booking = TestData.Reserve(apartment, Today.AddDays(3), 2, UtcNow, fees: Fees);
        booking.ConfirmPayment("pi_test", booking.TotalPrice.Amount, UtcNow);

        booking.Cancel(UtcNow).Value.RefundAmount.Should().Be(0);

        booking.RefundAmount.Should().Be(0);
        booking.RefundStatus.Should().Be(RefundStatus.None);
        booking.DomainEvents.Should().NotContain(e => e is BookingRefundRequestedDomainEvent);
    }

    [Fact]
    public void CancelByHost_Should_RefundEverything_AndCountAsAHostCancellation()
    {
        Booking booking = PaidBooking(CancellationPolicy.Strict, Today.AddDays(2));

        RefundQuote quote = booking.CancelByHost(UtcNow).Value;

        quote.RefundPercent.Should().Be(100);
        booking.RefundAmount.Should().Be(544);
        booking.RefundStatus.Should().Be(RefundStatus.Pending);
        booking.CancelledByHost.Should().BeTrue();
        booking.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public void CancelByHost_Should_Fail_ForUnpaidOrStartedBookings()
    {
        Apartment apartment = TestData.CreateApartment(TestData.Details(100, 50));
        Booking reserved = TestData.Reserve(apartment, Today.AddDays(5), 2, UtcNow, fees: Fees);

        reserved.CancelByHost(UtcNow).Error.Should().Be(BookingErrors.NotConfirmed);

        Booking started = PaidBooking(CancellationPolicy.Flexible, Today.AddDays(-1));
        started.CancelByHost(UtcNow).Error.Should().Be(BookingErrors.AlreadyStarted);
    }

    [Fact]
    public void MarkRefundSucceeded_Should_OnlyApplyToPendingRefunds()
    {
        Booking booking = PaidBooking(CancellationPolicy.Flexible, Today.AddDays(5));
        booking.Cancel(UtcNow);

        booking.MarkRefundSucceeded("re_1", UtcNow);
        booking.MarkRefundFailed();

        booking.RefundStatus.Should().Be(RefundStatus.Succeeded);
        booking.RefundId.Should().Be("re_1");
        booking.RefundedOnUtc.Should().Be(UtcNow);
    }

    [Fact]
    public void Timeline_Should_DescribeEachPolicy()
    {
        DateOnly checkIn = new(2026, 7, 20);

        RefundPolicy.Timeline(CancellationPolicy.Flexible, checkIn).Should().Equal(
            new RefundTier(new DateOnly(2026, 7, 19), 100),
            new RefundTier(null, 0));
        RefundPolicy.Timeline(CancellationPolicy.Moderate, checkIn).Should().Equal(
            new RefundTier(new DateOnly(2026, 7, 15), 100),
            new RefundTier(null, 50));
        RefundPolicy.Timeline(CancellationPolicy.Strict, checkIn).Should().Equal(
            new RefundTier(new DateOnly(2026, 7, 6), 100),
            new RefundTier(new DateOnly(2026, 7, 13), 50),
            new RefundTier(null, 0));
    }

    [Fact]
    public void Booking_Should_SnapshotThePolicy()
    {
        Apartment apartment = TestData.CreateApartment(TestData.Details(100, 0) with
        {
            CancellationPolicy = CancellationPolicy.Strict,
        });
        Booking booking = TestData.Reserve(apartment, Today.AddDays(30), 2, UtcNow);

        apartment.Update(TestData.Details(100, 0) with { CancellationPolicy = CancellationPolicy.Flexible });

        booking.CancellationPolicy.Should().Be(CancellationPolicy.Strict);
        apartment.CancellationPolicy.Should().Be(CancellationPolicy.Flexible);
    }

    private static Booking PaidBooking(CancellationPolicy policy, DateOnly start, bool recordPayment = true)
    {
        Apartment apartment = TestData.CreateApartment(TestData.Details(100, 50) with { CancellationPolicy = policy });
        Booking booking = TestData.Reserve(apartment, start, 4, UtcNow.AddDays(-60), fees: Fees);

        booking.TotalPrice.Amount.Should().Be(544);

        if (recordPayment)
        {
            booking.ConfirmPayment("pi_test", booking.TotalPrice.Amount, UtcNow.AddDays(-60)).IsSuccess.Should().BeTrue();
        }
        else
        {
            booking.Confirm(UtcNow.AddDays(-60)).IsSuccess.Should().BeTrue();
        }

        booking.ClearDomainEvents();

        return booking;
    }
}
