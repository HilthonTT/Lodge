using Lodge.Domain.Apartments;
using Lodge.Domain.Messaging;
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
