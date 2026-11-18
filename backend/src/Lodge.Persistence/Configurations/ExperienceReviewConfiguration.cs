using Lodge.Domain.Experiences;
using Lodge.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lodge.Persistence.Configurations;

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
