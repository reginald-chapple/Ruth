using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Identity.Domain;

namespace Ruth.Modules.Identity.Infrastructure.Configurations;

internal sealed class RestApiKeyConfiguration : IEntityTypeConfiguration<RestApiKey>
{
    public void Configure(EntityTypeBuilder<RestApiKey> builder)
    {
        _ = builder.ToTable("rest_api_keys");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        _ = builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
        _ = builder
            .Property(x => x.KeyPrefix)
            .HasColumnName("key_prefix")
            .HasMaxLength(16)
            .IsRequired();
        _ = builder
            .Property(x => x.KeyHash)
            .HasColumnName("key_hash")
            .HasMaxLength(128)
            .IsRequired();
        _ = builder.HasIndex(x => x.KeyHash).IsUnique();
        _ = builder.Property(x => x.UserAccountId).HasColumnName("user_account_id").IsRequired();
        _ = builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        _ = builder.Property(x => x.ExpiresOnUtc).HasColumnName("expires_on_utc");
        _ = builder.Property(x => x.LastUsedOnUtc).HasColumnName("last_used_on_utc");
        _ = builder.Property(x => x.RevokedOnUtc).HasColumnName("revoked_on_utc");
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");
        _ = builder
            .Property(x => x.ConcurrencyStamp)
            .HasColumnName("concurrency_stamp")
            .IsRequired();

        _ = builder
            .HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(x => x.UserAccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
