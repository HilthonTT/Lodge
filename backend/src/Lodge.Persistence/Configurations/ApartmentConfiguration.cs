using Lodge.Domain.Apartments;
using Lodge.Domain.Shared;
using Lodge.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lodge.Persistence.Configurations;

/// <summary>
/// Represents the configuration for the <see cref="Apartment"/> entity.
/// </summary>
internal sealed class ApartmentConfiguration : IEntityTypeConfiguration<Apartment>
{
    public void Configure(EntityTypeBuilder<Apartment> builder)
    {
        builder.HasKey(apartment => apartment.Id);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(apartment => apartment.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(apartment => apartment.Address);

        builder.OwnsOne(apartment => apartment.Name, nameBuilder =>
        {
            nameBuilder.WithOwner();

            nameBuilder.Property(name => name.Value)
                .HasMaxLength(Name.MaxLength)
                .HasColumnName("name")
                .IsRequired();
        });

        builder.OwnsOne(apartment => apartment.Description, descriptionBuilder =>
        {
            descriptionBuilder.WithOwner();

            descriptionBuilder.Property(description => description.Value)
                .HasMaxLength(Description.MaxLength)
                .HasColumnName("description")
                .IsRequired();
        });

        builder.OwnsOne(apartment => apartment.Price, priceBuilder =>
        {
            priceBuilder.WithOwner();

            priceBuilder.Property(money => money.Currency)
                .HasConversion(currency => currency.Code, code => Currency.FromCode(code));

            priceBuilder.HasIndex(money => money.Amount);
        });

        builder.OwnsOne(apartment => apartment.CleaningFee, cleaningFeeBuilder =>
        {
            cleaningFeeBuilder.WithOwner();

            cleaningFeeBuilder.Property(money => money.Currency)
                .HasConversion(currency => currency.Code, code => Currency.FromCode(code));
        });

        builder.Property(apartment => apartment.WeekendPrice).HasColumnName("weekend_price");

        builder.Property(apartment => apartment.Bathrooms).HasPrecision(4, 1);

        builder.Property(apartment => apartment.HouseRules).HasMaxLength(Apartment.TextMaxLength);

        builder.Property(apartment => apartment.CheckInInstructions).HasMaxLength(Apartment.TextMaxLength);

        // "Must have all of these amenities" filters use the array containment operator (@>).
        builder.HasIndex(apartment => apartment.Amenities).HasMethod("gin");

        builder.HasIndex(apartment => new { apartment.Latitude, apartment.Longitude });

        builder.OwnsMany(apartment => apartment.Images, imageBuilder =>
        {
            imageBuilder.ToTable("apartment_images");

            imageBuilder.WithOwner().HasForeignKey("ApartmentId");

            imageBuilder.HasKey(image => image.Id);

            imageBuilder.Property(image => image.Id).ValueGeneratedNever();

            imageBuilder.Property(image => image.Path).HasMaxLength(512);

            imageBuilder.Ignore(image => image.IsUploadedFile);

            imageBuilder.HasIndex("ApartmentId", nameof(ApartmentImage.Position));
        });

        builder.Navigation(apartment => apartment.Images)
            .HasField("_images")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(apartment => apartment.Deleted).HasDefaultValue(false);

        builder.HasQueryFilter(apartment => !apartment.Deleted);

        builder.Property<uint>("Version").IsRowVersion();
    }
}
