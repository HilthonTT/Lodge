using Lodge.Domain.Experiences;
using Lodge.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lodge.Persistence.Configurations;

/// <summary>
/// Represents the configuration for the <see cref="Experience"/> entity.
/// </summary>
internal sealed class ExperienceConfiguration : IEntityTypeConfiguration<Experience>
{
    public void Configure(EntityTypeBuilder<Experience> builder)
    {
        builder.HasKey(experience => experience.Id);

        builder.Property(experience => experience.Title).HasMaxLength(Experience.TitleMaxLength).IsRequired();
        builder.Property(experience => experience.Description).HasMaxLength(Experience.DescriptionMaxLength).IsRequired();
        builder.Property(experience => experience.Category).HasConversion<string>().HasMaxLength(20);
        builder.Property(experience => experience.City).HasMaxLength(Experience.PlaceMaxLength).IsRequired();
        builder.Property(experience => experience.Country).HasMaxLength(Experience.PlaceMaxLength).IsRequired();
        builder.Property(experience => experience.MeetingPoint).HasMaxLength(Experience.MeetingPointMaxLength).IsRequired();
        builder.Property(experience => experience.PricePerGuest).HasPrecision(12, 2);
        builder.Property(experience => experience.Currency).HasMaxLength(3).IsRequired();

        // Languages and included items are text[], the photo ids uuid[] (in display order).
        builder.Property(experience => experience.Languages);
        builder.Property(experience => experience.Included);
        builder.Property(experience => experience.ImageIds);

        builder.Property(experience => experience.Deleted).HasDefaultValue(false);

        builder.HasQueryFilter(experience => !experience.Deleted);

        builder.HasIndex(experience => experience.HostId);
        builder.HasIndex(experience => new { experience.IsListed, experience.Category });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(experience => experience.HostId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// Represents the configuration for the <see cref="ExperienceSession"/> entity.
/// </summary>
internal sealed class ExperienceSessionConfiguration : IEntityTypeConfiguration<ExperienceSession>
{
    public void Configure(EntityTypeBuilder<ExperienceSession> builder)
    {
        builder.HasKey(session => session.Id);

        builder.Ignore(session => session.IsCancelled);
        builder.Ignore(session => session.SpotsLeft);

        builder.HasIndex(session => new { session.ExperienceId, session.StartsAtUtc });
        builder.HasIndex(session => session.StartsAtUtc);

        builder.HasOne<Experience>()
            .WithMany()
            .HasForeignKey(session => session.ExperienceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

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

/// <summary>
/// Represents the configuration for the <see cref="ExperienceReview"/> entity.
/// </summary>
internal sealed class ExperienceReviewConfiguration : IEntityTypeConfiguration<ExperienceReview>
{
    public void Configure(EntityTypeBuilder<ExperienceReview> builder)
    {
        builder.HasKey(review => review.Id);

        builder.Property(review => review.Comment).HasMaxLength(Domain.Reviews.Comment.MaxLength).IsRequired();

        // One review per booking.
        builder.HasIndex(review => review.ExperienceBookingId).IsUnique();
        builder.HasIndex(review => new { review.ExperienceId, review.CreatedOnUtc });

        builder.HasOne<ExperienceBooking>()
            .WithMany()
            .HasForeignKey(review => review.ExperienceBookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Experience>()
            .WithMany()
            .HasForeignKey(review => review.ExperienceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(review => review.GuestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
