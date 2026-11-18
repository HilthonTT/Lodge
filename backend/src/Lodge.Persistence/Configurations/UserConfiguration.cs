using Lodge.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lodge.Persistence.Configurations;

/// <summary>
/// Represents the configuration for the <see cref="User"/> entity.
/// </summary>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(user => user.Id);

        builder.OwnsOne(user => user.FirstName, firstNameBuilder =>
        {
            firstNameBuilder.WithOwner();

            firstNameBuilder.Property(firstName => firstName.Value)
                .HasMaxLength(FirstName.MaxLength)
                .HasColumnName("first_name")
                .IsRequired();
        });

        builder.OwnsOne(user => user.LastName, lastNameBuilder =>
        {
            lastNameBuilder.WithOwner();

            lastNameBuilder.Property(lastName => lastName.Value)
                .HasMaxLength(LastName.MaxLength)
                .HasColumnName("last_name")
                .IsRequired();
        });

        builder.OwnsOne(user => user.Email, emailBuilder =>
        {
            emailBuilder.WithOwner();

            emailBuilder.Property(email => email.Value)
                .HasMaxLength(Email.MaxLength)
                .HasColumnName("email")
                .IsRequired();

            emailBuilder
                .HasIndex(emailValue => emailValue.Value)
                .IsUnique();
        });

        // Null for accounts created through a social login that never set a password.
        builder.Property<string?>("_passwordHash")
            .HasField("_passwordHash")
            .HasColumnName("password_hash");

        builder.Property(user => user.EmailVerifiedOnUtc);

        builder.Property(user => user.Bio).HasMaxLength(User.BioMaxLength);

        // Encrypted with ASP.NET Core Data Protection.
        builder.Property(user => user.TwoFactorSecret).HasMaxLength(1024);

        builder.Property(user => user.TwoFactorEnabledOnUtc);

        builder.Property(user => user.TwoFactorLastUsedStep);

        builder.Property(user => user.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnName("role")
            .IsRequired();

        builder.Property(user => user.IsSuperhost);

        builder.Property(user => user.CreatedOnUtc).IsRequired();

        builder.Property(user => user.ModifiedOnUtc);

        builder.Property(user => user.Deleted).HasDefaultValue(false);

        builder.HasQueryFilter(user => !user.Deleted);

        builder.Ignore(user => user.FullName);
        builder.Ignore(user => user.IsEmailVerified);
        builder.Ignore(user => user.HasPassword);
        builder.Ignore(user => user.TwoFactorEnabled);
    }
}
