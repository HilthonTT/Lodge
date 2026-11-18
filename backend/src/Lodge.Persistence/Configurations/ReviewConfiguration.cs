using Lodge.Domain.Apartments;
using Lodge.Domain.Bookings;
using Lodge.Domain.Reviews;
using Lodge.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lodge.Persistence.Configurations;

/// <summary>
/// Represents the configuration for the <see cref="Review"/> entity.
/// </summary>
internal sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(review => review.Id);

        // A complex property (rather than a value converter) so that queries can aggregate on the rating value.
        builder.ComplexProperty(review => review.Rating, ratingBuilder =>
            ratingBuilder.Property(rating => rating.Value)
                .HasColumnName("rating")
                .IsRequired());

        builder.Property(review => review.Comment)
            .HasMaxLength(Comment.MaxLength)
            .HasConversion(comment => comment.Value, value => Comment.Create(value).Value);

        builder.HasOne<Apartment>()
            .WithMany()
            .HasForeignKey(review => review.ApartmentId);

        builder.HasOne<Booking>()
            .WithMany()
            .HasForeignKey(review => review.BookingId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(review => review.UserId);
    }
}
