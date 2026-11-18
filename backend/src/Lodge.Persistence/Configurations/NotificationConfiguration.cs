using Lodge.Domain.Notifications;
using Lodge.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lodge.Persistence.Configurations;

/// <summary>
/// Represents the configuration for the <see cref="Notification"/> entity.
/// </summary>
internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(notification => notification.Id);

        builder.Property(notification => notification.Type).HasMaxLength(Notification.TypeMaxLength).IsRequired();

        builder.Property(notification => notification.Data).HasColumnType("jsonb").IsRequired();

        builder.Property(notification => notification.Link).HasMaxLength(Notification.LinkMaxLength).IsRequired();

        builder.Property(notification => notification.DedupeKey)
            .HasMaxLength(Notification.DedupeKeyMaxLength)
            .IsRequired();

        // At most one notification per source event and user.
        builder.HasIndex(notification => new { notification.UserId, notification.DedupeKey }).IsUnique();
        builder.HasIndex(notification => new { notification.UserId, notification.CreatedOnUtc });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(notification => notification.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
