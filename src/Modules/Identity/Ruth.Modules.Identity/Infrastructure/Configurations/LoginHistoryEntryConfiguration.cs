using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Identity.Domain;

namespace Ruth.Modules.Identity.Infrastructure.Configurations;

internal sealed class LoginHistoryEntryConfiguration : IEntityTypeConfiguration<LoginHistoryEntry>
{
    public void Configure(EntityTypeBuilder<LoginHistoryEntry> builder)
    {
        _ = builder.ToTable("login_history_entries");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.UserAccountId).HasColumnName("user_account_id");
        _ = builder
            .Property(x => x.UserNameAttempted)
            .HasColumnName("user_name_attempted")
            .HasMaxLength(256)
            .IsRequired();
        _ = builder
            .Property(x => x.AuthMethod)
            .HasColumnName("auth_method")
            .HasMaxLength(50)
            .IsRequired();
        _ = builder.Property(x => x.ProviderKey).HasColumnName("provider_key").HasMaxLength(64);
        _ = builder.Property(x => x.Succeeded).HasColumnName("succeeded").IsRequired();
        _ = builder
            .Property(x => x.FailureReason)
            .HasColumnName("failure_reason")
            .HasMaxLength(200);
        _ = builder.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(64);
        _ = builder.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(512);
        _ = builder.Property(x => x.OccurredOnUtc).HasColumnName("occurred_on_utc").IsRequired();

        _ = builder.HasIndex(x => x.OccurredOnUtc);
        _ = builder.HasIndex(x => x.UserAccountId);
    }
}
