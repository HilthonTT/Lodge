using Lodge.Domain.Experiences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lodge.Persistence.Configurations;

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
