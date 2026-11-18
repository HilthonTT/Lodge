using Lodge.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lodge.Persistence.Configurations;

/// <summary>
/// Represents the configuration for the <see cref="ExternalLogin"/> entity.
/// </summary>
internal sealed class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(EntityTypeBuilder<ExternalLogin> builder)
    {
        builder.ToTable("external_logins");

        builder.HasKey(login => login.Id);

        builder.Property(login => login.Provider)
            .HasMaxLength(ExternalLogin.ProviderMaxLength)
            .IsRequired();

        builder.Property(login => login.ProviderKey)
            .HasMaxLength(ExternalLogin.ProviderKeyMaxLength)
            .IsRequired();

        builder.HasIndex(login => new { login.Provider, login.ProviderKey }).IsUnique();

        // One account per provider and user.
        builder.HasIndex(login => new { login.UserId, login.Provider }).IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(login => login.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
