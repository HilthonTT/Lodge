using Lodge.Domain.Users;
using Lodge.Domain.Wishlists;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lodge.Persistence.Configurations;

/// <summary>
/// Represents the configuration for the <see cref="Wishlist"/> entity.
/// </summary>
internal sealed class WishlistConfiguration : IEntityTypeConfiguration<Wishlist>
{
    public void Configure(EntityTypeBuilder<Wishlist> builder)
    {
        builder.HasKey(wishlist => wishlist.Id);

        builder.Property(wishlist => wishlist.Name).HasMaxLength(Wishlist.NameMaxLength).IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(wishlist => wishlist.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsMany(wishlist => wishlist.Items, itemBuilder =>
        {
            itemBuilder.ToTable("wishlist_items");

            itemBuilder.WithOwner().HasForeignKey("WishlistId");

            itemBuilder.HasKey("WishlistId", nameof(WishlistItem.ApartmentId));

            itemBuilder.HasIndex(item => item.ApartmentId);
        });

        builder.Navigation(wishlist => wishlist.Items)
            .HasField("_items")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
