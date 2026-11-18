using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Shared;
using Lodge.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lodge.Persistence.Configurations;

/// <summary>
/// Represents the configuration for the <see cref="Booking"/> entity.
/// </summary>
internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasKey(booking => booking.Id);

        builder.OwnsOne(booking => booking.PriceForPeriod, priceBuilder =>
        {
            priceBuilder.WithOwner();

            priceBuilder.Property(money => money.Currency)
                .HasConversion(currency => currency.Code, code => Currency.FromCode(code));
        });

        builder.OwnsOne(booking => booking.Discount, priceBuilder =>
        {
            priceBuilder.WithOwner();

            priceBuilder.Property(money => money.Amount).HasColumnName("discount_amount");

            priceBuilder.Property(money => money.Currency)
                .HasColumnName("discount_currency")
                .HasConversion(currency => currency.Code, code => Currency.FromCode(code));
        });

        builder.OwnsOne(booking => booking.CleaningFee, priceBuilder =>
        {
            priceBuilder.WithOwner();

            priceBuilder.Property(money => money.Currency)
                .HasConversion(currency => currency.Code, code => Currency.FromCode(code));
        });

        builder.OwnsOne(booking => booking.AmenitiesUpCharge, priceBuilder =>
        {
            priceBuilder.WithOwner();

            priceBuilder.Property(money => money.Currency)
                .HasConversion(currency => currency.Code, code => Currency.FromCode(code));
        });

        builder.OwnsOne(booking => booking.ServiceFee, priceBuilder =>
        {
            priceBuilder.WithOwner();

            priceBuilder.Property(money => money.Amount).HasColumnName("service_fee_amount");

            priceBuilder.Property(money => money.Currency)
                .HasColumnName("service_fee_currency")
                .HasConversion(currency => currency.Code, code => Currency.FromCode(code));
        });

        builder.OwnsOne(booking => booking.Taxes, priceBuilder =>
        {
            priceBuilder.WithOwner();

            priceBuilder.Property(money => money.Amount).HasColumnName("taxes_amount");

            priceBuilder.Property(money => money.Currency)
                .HasColumnName("taxes_currency")
                .HasConversion(currency => currency.Code, code => Currency.FromCode(code));
        });

        builder.Property(booking => booking.TaxRatePercent).HasPrecision(5, 2);

        builder.Property(booking => booking.CancellationPolicy)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(booking => booking.PaymentIntentId).HasMaxLength(255);

        builder.Property(booking => booking.RefundStatus)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(booking => booking.RefundId).HasMaxLength(255);

        builder.OwnsOne(booking => booking.TotalPrice, priceBuilder =>

        {
            priceBuilder.WithOwner();

            priceBuilder.Property(money => money.Currency)
                .HasConversion(currency => currency.Code, code => Currency.FromCode(code));
        });

        builder.OwnsOne(booking => booking.Duration, durationBuilder =>
        {
            durationBuilder.Property(dateRange => dateRange.Start)
                .HasColumnName("duration_start");

            durationBuilder.Property(dateRange => dateRange.End)
                .HasColumnName("duration_end");
        });

        builder.Property(booking => booking.MessageToHost).HasMaxLength(Booking.MessageToHostMaxLength);

        // The expiry job looks for old booking requests; dashboards filter by apartment and status.
        builder.HasIndex(booking => new { booking.Status, booking.CreatedOnUtc });
        builder.HasIndex(booking => new { booking.ApartmentId, booking.Status });

        builder.HasOne<Apartment>()
            .WithMany()
            .HasForeignKey(booking => booking.ApartmentId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(booking => booking.UserId);
    }
}
