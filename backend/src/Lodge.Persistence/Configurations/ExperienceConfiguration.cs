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
