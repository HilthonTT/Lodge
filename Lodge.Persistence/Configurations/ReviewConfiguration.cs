using Lodge.Domain.Apartements;
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

        builder.OwnsOne(review => review.Rating, ratingBuilder =>
        {
            ratingBuilder.WithOwner();

            ratingBuilder.Property(rating => rating)
                .HasConversion(rating => rating.Value, value => Rating.Create(value).Value)
                .IsRequired();
        });

        builder.OwnsOne(review => review.Comment, commentBuilder =>
        {
            commentBuilder.WithOwner();

            commentBuilder.Property(comment => comment)
                .HasConversion(comment => comment.Value, value => Comment.Create(value).Value)
                .HasMaxLength(Comment.MaxLength)
                .IsRequired();
        });

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
