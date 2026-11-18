using Lodge.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lodge.Persistence.Configurations;

/// <summary>
/// Represents the configuration for the <see cref="UserToken"/> entity.
/// </summary>
internal sealed class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
{
    public void Configure(EntityTypeBuilder<UserToken> builder)
    {
        builder.ToTable("user_tokens");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.Purpose)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        // Lower-case hex SHA-256.
        builder.Property(token => token.TokenHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(token => token.TokenHash).IsUnique();

        builder.HasIndex(token => new { token.UserId, token.Purpose, token.CreatedOnUtc });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
