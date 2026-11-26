using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Tenancy.Domain;

namespace Ruth.Modules.Tenancy.Infrastructure.Configurations;

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        _ = builder.ToTable("tenants");
        _ = builder.HasKey(x => x.Id);
        ConfigureIdentityProperties(builder);
        ConfigureComplianceAndAuditProperties(builder);

        _ = builder.HasIndex(x => x.Slug).IsUnique();
        _ = builder.HasIndex(x => x.Ein).IsUnique();
        _ = builder.HasIndex(x => x.LifecycleStage);
        _ = builder.Ignore(x => x.DomainEvents);
    }

    private static void ConfigureIdentityProperties(EntityTypeBuilder<Tenant> builder)
    {
        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(64).IsRequired();
        _ = builder
            .Property(x => x.LegalName)
            .HasColumnName("legal_name")
            .HasMaxLength(200)
            .IsRequired();
        _ = builder
            .Property(x => x.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(200)
            .IsRequired();
        _ = builder.Property(x => x.Ein).HasColumnName("ein").HasMaxLength(16).IsRequired();
        _ = builder
            .Property(x => x.IrsSubsectionCode)
            .HasColumnName("irs_subsection_code")
            .HasMaxLength(32)
            .IsRequired();
        _ = builder
            .Property(x => x.StateCode)
            .HasColumnName("state_code")
            .HasMaxLength(4)
            .IsRequired();
        _ = builder
            .Property(x => x.TimeZoneId)
            .HasColumnName("time_zone_id")
            .HasMaxLength(100)
            .IsRequired();
    }

    private static void ConfigureComplianceAndAuditProperties(EntityTypeBuilder<Tenant> builder)
    {
        _ = builder
            .Property(x => x.LifecycleStage)
            .HasColumnName("lifecycle_stage")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder
            .Property(x => x.IrsStatus)
            .HasColumnName("irs_status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder
            .Property(x => x.IrsOfficialName)
            .HasColumnName("irs_official_name")
            .HasMaxLength(250);
        _ = builder
            .Property(x => x.IrsDeductibilityCode)
            .HasColumnName("irs_deductibility_code")
            .HasMaxLength(32);
        _ = builder.Property(x => x.IrsVerifiedOnUtc).HasColumnName("irs_verified_on_utc");
        _ = builder
            .Property(x => x.StripeConnectAccountId)
            .HasColumnName("stripe_connect_account_id")
            .HasMaxLength(128);
        _ = builder.Property(x => x.StripeChargesEnabled).HasColumnName("stripe_charges_enabled");
        _ = builder.Property(x => x.StripePayoutsEnabled).HasColumnName("stripe_payouts_enabled");
        _ = builder
            .Property(x => x.SubmittedForReviewOnUtc)
            .HasColumnName("submitted_for_review_on_utc");
        _ = builder.Property(x => x.ApprovedOnUtc).HasColumnName("approved_on_utc");
        _ = builder
            .Property(x => x.ApprovedByOperatorEmail)
            .HasColumnName("approved_by_operator_email")
            .HasMaxLength(256);
        _ = builder.Property(x => x.ReviewNotes).HasColumnName("review_notes").HasMaxLength(2000);
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");
        _ = builder
            .Property(x => x.ConcurrencyStamp)
            .HasColumnName("concurrency_stamp")
            .IsConcurrencyToken()
            .IsRequired();
    }
}
