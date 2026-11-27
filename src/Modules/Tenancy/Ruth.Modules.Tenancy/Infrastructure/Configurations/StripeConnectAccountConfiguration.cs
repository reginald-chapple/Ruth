using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Tenancy.Domain;

namespace Ruth.Modules.Tenancy.Infrastructure.Configurations;

internal sealed class StripeConnectAccountConfiguration
    : IEntityTypeConfiguration<StripeConnectAccount>
{
    public void Configure(EntityTypeBuilder<StripeConnectAccount> builder)
    {
        _ = builder.ToTable("stripe_connect_accounts");
        _ = builder.HasKey(x => x.Id);
        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.TargetTenantId).HasColumnName("target_tenant_id").IsRequired();
        _ = builder
            .Property(x => x.ExternalAccountId)
            .HasColumnName("external_account_id")
            .HasMaxLength(128)
            .IsRequired();
        _ = builder.Property(x => x.IsMockAccount).HasColumnName("is_mock_account").IsRequired();
        _ = builder.Property(x => x.ChargesEnabled).HasColumnName("charges_enabled").IsRequired();
        _ = builder.Property(x => x.PayoutsEnabled).HasColumnName("payouts_enabled").IsRequired();
        _ = builder
            .Property(x => x.DetailsSubmitted)
            .HasColumnName("details_submitted")
            .IsRequired();
        _ = builder
            .Property(x => x.LastOnboardingUrl)
            .HasColumnName("last_onboarding_url")
            .HasMaxLength(2048);
        _ = builder
            .Property(x => x.OnboardingExpiresAtUtc)
            .HasColumnName("onboarding_expires_at_utc");
        _ = builder
            .Property(x => x.LastSyncedOnUtc)
            .HasColumnName("last_synced_on_utc")
            .IsRequired();
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");
        _ = builder
            .Property(x => x.ConcurrencyStamp)
            .HasColumnName("concurrency_stamp")
            .IsConcurrencyToken()
            .IsRequired();

        _ = builder.HasIndex(x => x.TargetTenantId).IsUnique();
        _ = builder.HasIndex(x => x.ExternalAccountId).IsUnique();
        _ = builder.Ignore(x => x.DomainEvents);
        _ = builder
            .HasOne<Tenant>()
            .WithOne()
            .HasForeignKey<StripeConnectAccount>(x => x.TargetTenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
