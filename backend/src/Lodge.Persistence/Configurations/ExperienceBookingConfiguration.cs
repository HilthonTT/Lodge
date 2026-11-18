using Lodge.Domain.Experiences;
using Lodge.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lodge.Persistence.Configurations;

/// <summary>
/// Represents the configuration for the <see cref="ExperienceBooking"/> entity.
/// </summary>
internal sealed class ExperienceBookingConfiguration : IEntityTypeConfiguration<ExperienceBooking>
{
    public void Configure(EntityTypeBuilder<ExperienceBooking> builder)
    {
        builder.HasKey(booking => booking.Id);

        builder.Property(booking => booking.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(booking => booking.RefundStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(booking => booking.PriceTotal).HasPrecision(12, 2);
        builder.Property(booking => booking.ServiceFee).HasPrecision(12, 2);
        builder.Property(booking => booking.TotalPrice).HasPrecision(12, 2);
        builder.Property(booking => booking.AmountPaid).HasPrecision(12, 2);
        builder.Property(booking => booking.RefundAmount).HasPrecision(12, 2);
        builder.Property(booking => booking.Currency).HasMaxLength(3).IsRequired();
        builder.Property(booking => booking.PaymentIntentId).HasMaxLength(255);
        builder.Property(booking => booking.RefundId).HasMaxLength(255);

        builder.Ignore(booking => booking.FreeCancellationUntilUtc);
        builder.Ignore(booking => booking.IsPaid);

        builder.HasIndex(booking => booking.GuestId);
        builder.HasIndex(booking => booking.SessionId);
        builder.HasIndex(booking => booking.ExperienceId);
        builder.HasIndex(booking => new { booking.Status, booking.SessionStartsAtUtc });

        builder.HasOne<ExperienceSession>()
            .WithMany()
            .HasForeignKey(booking => booking.SessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Experience>()
            .WithMany()
            .HasForeignKey(booking => booking.ExperienceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(booking => booking.GuestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
