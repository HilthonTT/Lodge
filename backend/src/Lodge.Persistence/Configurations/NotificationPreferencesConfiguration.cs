using Lodge.Domain.Notifications;
using Lodge.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lodge.Persistence.Configurations;

/// <summary>
/// Represents the configuration for the <see cref="NotificationPreferences"/> entity (keyed by the user id).
/// </summary>
internal sealed class NotificationPreferencesConfiguration : IEntityTypeConfiguration<NotificationPreferences>
{
    public void Configure(EntityTypeBuilder<NotificationPreferences> builder)
    {
        builder.ToTable("notification_preferences");

        builder.HasKey(preferences => preferences.Id);

        builder.Property(preferences => preferences.Id).HasColumnName("user_id").ValueGeneratedNever();

        builder.Ignore(preferences => preferences.UserId);

        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<NotificationPreferences>(preferences => preferences.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
