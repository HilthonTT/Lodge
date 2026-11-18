using Lodge.Domain.Bookings;
using Lodge.Domain.Experiences;
using Lodge.Domain.Payouts;
using Lodge.Domain.Shared;
using Lodge.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lodge.Persistence.Configurations;

/// <summary>
/// Represents the configuration for the <see cref="Payout"/> entity.
/// </summary>
internal sealed class PayoutConfiguration : IEntityTypeConfiguration<Payout>
{
    public void Configure(EntityTypeBuilder<Payout> builder)
    {
        builder.HasKey(payout => payout.Id);

        builder.OwnsOne(payout => payout.Amount, amountBuilder =>
        {
            amountBuilder.WithOwner();

            amountBuilder.Property(money => money.Currency)
                .HasConversion(currency => currency.Code, code => Currency.FromCode(code));
        });

        builder.Property(payout => payout.HostFeePercent).HasPrecision(5, 2);

        builder.Property(payout => payout.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(payout => payout.TransferId).HasMaxLength(255);

        builder.Property(payout => payout.FailureReason).HasMaxLength(500);

        // One payout per booking (or experience booking); the daily job looks for due payouts.
        builder.HasIndex(payout => payout.BookingId).IsUnique();
        builder.HasIndex(payout => payout.ExperienceBookingId).IsUnique();
        builder.HasIndex(payout => new { payout.Status, payout.ScheduledFor });
        builder.HasIndex(payout => payout.HostId);

        builder.HasOne<Booking>().WithMany().HasForeignKey(payout => payout.BookingId);
        builder.HasOne<ExperienceBooking>().WithMany().HasForeignKey(payout => payout.ExperienceBookingId);

        // A payout pays exactly one stay or one experience booking.
        builder.ToTable(table => table.HasCheckConstraint(
            "ck_payouts_one_source",
            "(booking_id IS NULL) <> (experience_booking_id IS NULL)"));
        builder.HasOne<User>().WithMany().HasForeignKey(payout => payout.HostId).OnDelete(DeleteBehavior.Restrict);
    }
}
