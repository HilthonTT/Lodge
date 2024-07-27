using Lodge.Domain.Apartements;
using Lodge.Domain.Shared;
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

        builder.OwnsOne(apartment => apartment.Address);

        builder.OwnsOne(apartment => apartment.Name, nameBuilder =>
        {
            nameBuilder.WithOwner();

            nameBuilder.Property(name => name.Value)
                .HasMaxLength(Name.MaxLength)
                .IsRequired();
        });

        builder.OwnsOne(apartment => apartment.Description, descriptionBuilder =>
        {
            descriptionBuilder.WithOwner();

            descriptionBuilder.Property(description => description.Value)
                .HasMaxLength(Description.MaxLength)
                .IsRequired();
        });

        builder.OwnsOne(apartment => apartment.Price, priceBuilder =>
        {
            priceBuilder.WithOwner();

            priceBuilder.Property(money => money.Currency)
                .HasConversion(currency => currency.Code, code => Currency.FromCode(code));
        });

        builder.OwnsOne(apartment => apartment.CleaningFee, cleaningFeeBuilder =>
        {
            cleaningFeeBuilder.WithOwner();

            cleaningFeeBuilder.Property(money => money.Currency)
                .HasConversion(currency => currency.Code, code => Currency.FromCode(code));
        });

        builder.Property<uint>("Version").IsRowVersion();
    }
}
