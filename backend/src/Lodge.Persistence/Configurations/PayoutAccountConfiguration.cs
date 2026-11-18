using Lodge.Domain.Payouts;
using Lodge.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lodge.Persistence.Configurations;

/// <summary>
/// Represents the configuration for the <see cref="PayoutAccount"/> entity.
/// </summary>
internal sealed class PayoutAccountConfiguration : IEntityTypeConfiguration<PayoutAccount>
{
    public void Configure(EntityTypeBuilder<PayoutAccount> builder)
    {
        builder.HasKey(account => account.Id);

        builder.Property(account => account.ExternalAccountId).HasMaxLength(255);

        builder.Property(account => account.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(account => account.UserId).IsUnique();
        builder.HasIndex(account => account.ExternalAccountId).IsUnique();

        builder.HasOne<User>().WithMany().HasForeignKey(account => account.UserId);
    }
}
