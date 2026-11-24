using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Identity.Domain;

namespace Ruth.Modules.Identity.Infrastructure.Configurations;

internal sealed class OrganizationProfileConfiguration
    : IEntityTypeConfiguration<OrganizationProfile>
{
    public void Configure(EntityTypeBuilder<OrganizationProfile> builder)
    {
        _ = builder.ToTable("organization_profiles", static table =>
            table.HasCheckConstraint("ck_organization_profiles_singleton_key", "singleton_key = 1")
        );
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder
            .Property(x => x.SingletonKey)
            .HasColumnName("singleton_key")
            .HasDefaultValue(1)
            .IsRequired();
        _ = builder.HasIndex(x => x.SingletonKey).IsUnique();

        ConfigureOrganizationProperties(builder);
        ConfigureAuditProperties(builder);
    }

    private static void ConfigureOrganizationProperties(
        EntityTypeBuilder<OrganizationProfile> builder
    )
    {
        _ = builder
            .Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(OnboardingPolicy.OrganizationNameMaxLength)
            .IsRequired();
        _ = builder
            .Property(x => x.TimeZoneId)
            .HasColumnName("time_zone_id")
            .HasMaxLength(OnboardingPolicy.TimeZoneIdMaxLength)
            .IsRequired();
        _ = builder
            .Property(x => x.PrimaryCampusName)
            .HasColumnName("primary_campus_name")
            .HasMaxLength(OnboardingPolicy.CampusNameMaxLength)
            .IsRequired();
        _ = builder
            .Property(x => x.PrimaryCampusShortCode)
            .HasColumnName("primary_campus_short_code")
            .HasMaxLength(OnboardingPolicy.CampusShortCodeMaxLength);
    }

    private static void ConfigureAuditProperties(EntityTypeBuilder<OrganizationProfile> builder)
    {
        _ = builder
            .Property(x => x.IsOnboardingCompleted)
            .HasColumnName("is_onboarding_completed")
            .IsRequired();
        _ = builder
            .Property(x => x.SelfRegistrationLocked)
            .HasColumnName("self_registration_locked")
            .IsRequired();
        _ = builder.Property(x => x.CompletedOnUtc).HasColumnName("completed_on_utc").IsRequired();
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");
        _ = builder
            .Property(x => x.ConcurrencyStamp)
            .HasColumnName("concurrency_stamp")
            .IsRequired();
    }
}
