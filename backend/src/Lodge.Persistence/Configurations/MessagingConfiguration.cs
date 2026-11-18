using Lodge.Domain.Apartments;
using Lodge.Domain.Messaging;
using Lodge.Domain.Notifications;
using Lodge.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lodge.Persistence.Configurations;

/// <summary>
/// Represents the configuration for the <see cref="Conversation"/> entity.
/// </summary>
internal sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.HasKey(conversation => conversation.Id);

        // One conversation per (listing, guest).
        builder.HasIndex(conversation => new { conversation.ApartmentId, conversation.GuestId }).IsUnique();
        builder.HasIndex(conversation => new { conversation.GuestId, conversation.LastMessageOnUtc });
        builder.HasIndex(conversation => new { conversation.HostId, conversation.LastMessageOnUtc });

        builder.HasOne<Apartment>()
            .WithMany()
            .HasForeignKey(conversation => conversation.ApartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(conversation => conversation.GuestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(conversation => conversation.HostId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// Represents the configuration for the <see cref="Message"/> entity.
/// </summary>
internal sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(message => message.Id);

        builder.Property(message => message.Type).HasConversion<string>().HasMaxLength(10);

        builder.Property(message => message.Body).HasMaxLength(Message.BodyMaxLength).IsRequired();

        builder.Property(message => message.SystemEvent).HasMaxLength(Message.SystemEventMaxLength);

        // Cursor pagination walks (sent_on_utc, id) backwards within a conversation.
        builder.HasIndex(message => new { message.ConversationId, message.SentOnUtc, message.Id });

        builder.HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(message => message.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(message => message.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

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
