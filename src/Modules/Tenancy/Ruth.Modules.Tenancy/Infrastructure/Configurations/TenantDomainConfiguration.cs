using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Tenancy.Domain;

namespace Ruth.Modules.Tenancy.Infrastructure.Configurations;

internal sealed class TenantDomainConfiguration : IEntityTypeConfiguration<TenantDomain>
{
    public void Configure(EntityTypeBuilder<TenantDomain> builder)
    {
        _ = builder.ToTable("tenant_domains");
        _ = builder.HasKey(x => x.Id);
        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.TargetTenantId).HasColumnName("target_tenant_id").IsRequired();
        _ = builder
            .Property(x => x.Hostname)
            .HasColumnName("hostname")
            .HasMaxLength(255)
            .IsRequired();
        _ = builder.Property(x => x.IsCustomDomain).HasColumnName("is_custom_domain").IsRequired();
        _ = builder.Property(x => x.IsVerified).HasColumnName("is_verified").IsRequired();
        _ = builder.Property(x => x.IsPrimary).HasColumnName("is_primary").IsRequired();
        _ = builder
            .Property(x => x.MakePrimaryWhenVerified)
            .HasColumnName("make_primary_when_verified")
            .IsRequired();
        _ = builder
            .Property(x => x.VerificationTxtName)
            .HasColumnName("verification_txt_name")
            .HasMaxLength(300);
        _ = builder
            .Property(x => x.VerificationTxtValue)
            .HasColumnName("verification_txt_value")
            .HasMaxLength(128);
        _ = builder.Property(x => x.VerifiedOnUtc).HasColumnName("verified_on_utc");
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");

        _ = builder.HasIndex(x => x.Hostname).IsUnique();
        _ = builder.HasIndex(x => x.TargetTenantId);
        _ = builder
            .HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TargetTenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
