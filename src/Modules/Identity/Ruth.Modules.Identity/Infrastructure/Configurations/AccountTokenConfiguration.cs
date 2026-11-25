using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Identity.Domain;

namespace Ruth.Modules.Identity.Infrastructure.Configurations;

internal sealed class AccountTokenConfiguration : IEntityTypeConfiguration<AccountToken>
{
    public void Configure(EntityTypeBuilder<AccountToken> builder)
    {
        _ = builder.ToTable("account_tokens");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.UserAccountId).HasColumnName("user_account_id").IsRequired();
        _ = builder.Property(x => x.Purpose).HasColumnName("purpose").IsRequired();
        _ = builder
            .Property(x => x.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(128)
            .IsRequired();
        _ = builder.HasIndex(x => x.TokenHash).IsUnique();
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ExpiresOnUtc).HasColumnName("expires_on_utc").IsRequired();
        _ = builder.Property(x => x.ConsumedOnUtc).HasColumnName("consumed_on_utc");

        _ = builder
            .HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(x => x.UserAccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
