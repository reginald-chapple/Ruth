using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Tenancy.Domain;

namespace Ruth.Modules.Tenancy.Infrastructure.Configurations;

internal sealed class IrsExemptVerificationConfiguration
    : IEntityTypeConfiguration<IrsExemptVerification>
{
    public void Configure(EntityTypeBuilder<IrsExemptVerification> builder)
    {
        _ = builder.ToTable("irs_exempt_verifications");
        _ = builder.HasKey(x => x.Id);
        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.TargetTenantId).HasColumnName("target_tenant_id").IsRequired();
        _ = builder
            .Property(x => x.CheckedEin)
            .HasColumnName("checked_ein")
            .HasMaxLength(16)
            .IsRequired();
        _ = builder
            .Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder.Property(x => x.OfficialName).HasColumnName("official_name").HasMaxLength(250);
        _ = builder
            .Property(x => x.SubsectionCode)
            .HasColumnName("subsection_code")
            .HasMaxLength(32);
        _ = builder
            .Property(x => x.DeductibilityCode)
            .HasColumnName("deductibility_code")
            .HasMaxLength(32);
        _ = builder
            .Property(x => x.DiagnosticSummary)
            .HasColumnName("diagnostic_summary")
            .HasMaxLength(1000)
            .IsRequired();
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");

        _ = builder.HasIndex(x => new { x.TargetTenantId, x.CreatedOnUtc });
        _ = builder
            .HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TargetTenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
