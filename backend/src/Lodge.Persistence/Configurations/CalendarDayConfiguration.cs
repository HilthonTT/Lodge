using Lodge.Domain.Apartments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lodge.Persistence.Configurations;

/// <summary>
/// Represents the configuration for the <see cref="CalendarDay"/> entity.
/// </summary>
internal sealed class CalendarDayConfiguration : IEntityTypeConfiguration<CalendarDay>
{
    public void Configure(EntityTypeBuilder<CalendarDay> builder)
    {
        builder.ToTable("apartment_calendar_days");

        builder.HasKey(day => day.Id);

        builder.HasIndex(day => new { day.ApartmentId, day.Date }).IsUnique();

        builder.HasOne<Apartment>()
            .WithMany()
            .HasForeignKey(day => day.ApartmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(day => day.IsEmpty);
    }
}
