using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Identity.Domain;

namespace Ruth.Modules.Identity.Infrastructure.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        _ = builder.ToTable("refresh_tokens");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.UserAccountId).HasColumnName("user_account_id").IsRequired();
        _ = builder
            .Property(x => x.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(128)
            .IsRequired();
        _ = builder.HasIndex(x => x.TokenHash).IsUnique();
        _ = builder.Property(x => x.FamilyId).HasColumnName("family_id").IsRequired();
        _ = builder.HasIndex(x => x.FamilyId);
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ExpiresOnUtc).HasColumnName("expires_on_utc").IsRequired();
        _ = builder.Property(x => x.CreatedByIp).HasColumnName("created_by_ip").HasMaxLength(64);
        _ = builder.Property(x => x.RevokedOnUtc).HasColumnName("revoked_on_utc");
        _ = builder.Property(x => x.ReplacedByTokenId).HasColumnName("replaced_by_token_id");
        _ = builder
            .Property(x => x.RevocationReason)
            .HasColumnName("revocation_reason")
            .HasMaxLength(200);

        _ = builder
            .HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(x => x.UserAccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
