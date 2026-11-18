using Lodge.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lodge.Persistence.Configurations;

/// <summary>
/// Represents the configuration for the <see cref="RecoveryCode"/> entity.
/// </summary>
internal sealed class RecoveryCodeConfiguration : IEntityTypeConfiguration<RecoveryCode>
{
    public void Configure(EntityTypeBuilder<RecoveryCode> builder)
    {
        builder.ToTable("recovery_codes");

        builder.HasKey(code => code.Id);

        builder.Property(code => code.CodeHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(code => code.UserId);

        builder.Ignore(code => code.IsUsed);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(code => code.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
