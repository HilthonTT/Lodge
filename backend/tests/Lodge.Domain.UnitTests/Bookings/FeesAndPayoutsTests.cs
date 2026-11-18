using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Payouts;
using Lodge.Domain.Payouts.Events;

namespace Lodge.Domain.UnitTests.Bookings;

public class FeesAndPayoutsTests
{
    private static readonly DateTime UtcNow = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = DateOnly.FromDateTime(UtcNow);

    // A Monday, so no weekend prices are involved.
    private static readonly DateOnly Monday = new(2026, 6, 8);

    [Fact]
    public void ServiceFee_Should_ApplyToTheStaySubtotal_AndTaxesToTheNights()
    {
        Apartment apartment = TestData.CreateApartment(100, 30, Amenity.WiFi);
        DateRange stay = DateRange.Create(Monday, Monday.AddDays(3)).Value;

        PricingDetails details = new PricingService().CalculatePrice(apartment, stay, feeRates: new FeeRates(12, 12.5m));

        // Nights 300, WiFi up-charge 2% = 6, cleaning 30: subtotal 336.
        details.StaySubtotal.Amount.Should().Be(336);
        details.ServiceFee.Amount.Should().Be(40.32m);
        details.Taxes.Amount.Should().Be(37.50m);
        details.TaxRatePercent.Should().Be(12.5m);
        details.TotalPrice.Amount.Should().Be(336 + 40.32m + 37.50m);
    }

    [Fact]
    public void Taxes_Should_ApplyToTheNightsAfterTheDiscount()
    {
        Apartment apartment = TestData.CreateApartment(TestData.Details(100, 0) with { WeeklyDiscountPercent = 10 });
        DateRange week = DateRange.Create(Monday, Monday.AddDays(7)).Value;

        PricingDetails details = new PricingService().CalculatePrice(apartment, week, feeRates: new FeeRates(12, 10));

        details.Discount.Amount.Should().Be(70);
        details.StaySubtotal.Amount.Should().Be(630);
        details.ServiceFee.Amount.Should().Be(75.60m);
        details.Taxes.Amount.Should().Be(63);
        details.TotalPrice.Amount.Should().Be(768.60m);
    }

    [Fact]
    public void WithoutFeeRates_Should_ChargeNoServiceFeeAndNoTaxes()
    {
        Apartment apartment = TestData.CreateApartment(100, 20);

        PricingDetails details = new PricingService().CalculatePrice(apartment, DateRange.Create(Monday, Monday.AddDays(2)).Value);

        details.ServiceFee.Amount.Should().Be(0);
        details.Taxes.Amount.Should().Be(0);
        details.TotalPrice.Amount.Should().Be(220);
    }

    [Fact]
    public void Booking_Should_SnapshotTheFees_AndRecalculateThemOnADateChange()
    {
        Apartment apartment = TestData.CreateApartment(100, 0);
        var fees = new FeeRates(10, 5);
        Booking booking = TestData.Reserve(apartment, Monday, 2, UtcNow, fees: fees);

        booking.ServiceFee.Amount.Should().Be(20);
        booking.Taxes.Amount.Should().Be(10);
        booking.TaxRatePercent.Should().Be(5);
        booking.TotalPrice.Amount.Should().Be(230);

        DateRange longer = DateRange.Create(Monday, Monday.AddDays(4)).Value;
        booking.ChangeDates(apartment, longer, UtcNow, new PricingService().CalculatePrice(apartment, longer, feeRates: fees))
            .IsSuccess.Should().BeTrue();

        booking.ServiceFee.Amount.Should().Be(40);
        booking.Taxes.Amount.Should().Be(20);
        booking.TotalPrice.Amount.Should().Be(460);
    }

    [Theory]
    [InlineData(450, 3, 436.50)]
    [InlineData(333.33, 3, 323.33)]
    [InlineData(100, 0, 100)]
    [InlineData(0, 3, 0)]
    public void HostPayout_Should_DeductTheHostFee(decimal stayAmount, decimal feePercent, decimal expected)
    {
        HostFees.Payout(stayAmount, feePercent).Should().Be(expected);
    }

    [Fact]
    public void Payout_Should_BeTheStaySubtotalMinusTheHostFee_WithoutServiceFeeOrTaxes()
    {
        Apartment apartment = TestData.CreateApartment(100, 50);
        Booking booking = TestData.Reserve(apartment, Monday, 4, UtcNow, fees: new FeeRates(12, 10));
        booking.ConfirmPayment("pi_1", booking.TotalPrice.Amount, UtcNow);
        Guid hostId = Guid.NewGuid();

        var payout = Payout.Schedule(booking, hostId, 3);

        // Subtotal 450 (the 54 service fee and 40 taxes are not paid out), minus 3% = 13.50.
        payout.Amount.Amount.Should().Be(436.50m);
        payout.Amount.Currency.Code.Should().Be("USD");
        payout.HostId.Should().Be(hostId);
        payout.Status.Should().Be(PayoutStatus.Scheduled);
        payout.ScheduledFor.Should().Be(Monday.AddDays(1));
        payout.IsDue(Monday).Should().BeFalse();
        payout.IsDue(Monday.AddDays(1)).Should().BeTrue();
    }

    [Fact]
    public void Payout_Should_FollowTheCancellation()
    {
        Payout partial = NewPayout();
        partial.AdjustForCancellation(200);
        partial.Amount.Amount.Should().Be(194);
        partial.Status.Should().Be(PayoutStatus.Scheduled);

        Payout cancelled = NewPayout();
        cancelled.AdjustForCancellation(0);
        cancelled.Amount.Amount.Should().Be(0);
        cancelled.Status.Should().Be(PayoutStatus.Cancelled);

        Payout paid = NewPayout();
        paid.MarkPaid("tr_1", UtcNow).IsSuccess.Should().BeTrue();
        paid.AdjustForCancellation(0);
        paid.Status.Should().Be(PayoutStatus.Paid);
    }

    [Fact]
    public void Payout_Should_GoOnHold_AndBePaidLater()
    {
        Payout payout = NewPayout();

        payout.PutOnHold();
        payout.Status.Should().Be(PayoutStatus.OnHold);

        payout.MarkPaid("tr_1", UtcNow).IsSuccess.Should().BeTrue();
        payout.Status.Should().Be(PayoutStatus.Paid);
        payout.PaidOnUtc.Should().Be(UtcNow);
        payout.TransferId.Should().Be("tr_1");

        payout.MarkPaid("tr_2", UtcNow).Error.Should().Be(PayoutErrors.NotPayable);
    }

    [Fact]
    public void PayoutAccount_Should_RaiseAnEvent_WhenItBecomesActive()
    {
        Guid hostId = Guid.NewGuid();
        var account = PayoutAccount.Create(hostId, "acct_1");

        account.Status.Should().Be(PayoutAccountStatus.Pending);
        account.Update(payoutsEnabled: false);
        account.DomainEvents.Should().BeEmpty();

        account.Update(payoutsEnabled: true);
        account.Update(payoutsEnabled: true);

        account.CanReceivePayouts.Should().BeTrue();
        account.DomainEvents.Should().ContainSingle()
            .Which.Should().Be(new PayoutAccountActivatedDomainEvent(hostId));
    }

    private static Payout NewPayout()
    {
        Apartment apartment = TestData.CreateApartment(100, 50);
        Booking booking = TestData.Reserve(apartment, Today.AddDays(20), 4, UtcNow);
        booking.ConfirmPayment("pi_1", booking.TotalPrice.Amount, UtcNow);

        return Payout.Schedule(booking, Guid.NewGuid(), 3);
    }
}
