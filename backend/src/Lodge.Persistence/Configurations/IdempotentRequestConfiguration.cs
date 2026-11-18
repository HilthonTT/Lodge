using Lodge.Persistence.Idempotency;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lodge.Persistence.Configurations;

/// <summary>
/// Represents the configuration for the <see cref="IdempotentRequest"/> entity.
/// </summary>
internal sealed class IdempotentRequestConfiguration : IEntityTypeConfiguration<IdempotentRequest>
{
    public void Configure(EntityTypeBuilder<IdempotentRequest> builder)
    {
        builder.ToTable("idempotent_requests");

        builder.HasKey(idempotentRequest => idempotentRequest.Id);

        builder.Property(idempotentRequest => idempotentRequest.Name).IsRequired();

        // The serialized (JSON) result replayed for retries; null for keys recorded before results were stored.
        builder.Property(idempotentRequest => idempotentRequest.Response).HasColumnType("jsonb");
    }
}
