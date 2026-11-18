using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Reviews;
using Lodge.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lodge.Persistence.Configurations;

/// <summary>
/// Represents the configuration for the <see cref="GuestReview"/> entity (host to guest reviews).
/// </summary>
internal sealed class GuestReviewConfiguration : IEntityTypeConfiguration<GuestReview>
{
    public void Configure(EntityTypeBuilder<GuestReview> builder)
    {
        builder.ToTable("guest_reviews");

        builder.HasKey(review => review.Id);

        builder.ComplexProperty(review => review.Rating, ratingBuilder =>
            ratingBuilder.Property(rating => rating.Value)
                .HasColumnName("rating")
                .IsRequired());

        builder.Property(review => review.Comment)
            .HasMaxLength(Comment.MaxLength)
            .HasConversion(comment => comment.Value, value => Comment.Create(value).Value);

        builder.HasIndex(review => review.BookingId).IsUnique();
        builder.HasIndex(review => new { review.GuestId, review.IsPublished });

        builder.HasOne<Booking>().WithMany().HasForeignKey(review => review.BookingId);
        builder.HasOne<Apartment>().WithMany().HasForeignKey(review => review.ApartmentId);
        builder.HasOne<User>().WithMany().HasForeignKey(review => review.GuestId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(review => review.HostId).OnDelete(DeleteBehavior.Restrict);
    }
}
