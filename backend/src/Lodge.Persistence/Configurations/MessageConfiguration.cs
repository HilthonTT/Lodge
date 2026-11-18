using Lodge.Domain.Messaging;
using Lodge.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lodge.Persistence.Configurations;

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
