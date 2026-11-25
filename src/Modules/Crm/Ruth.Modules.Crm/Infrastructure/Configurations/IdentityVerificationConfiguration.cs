using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Crm.Domain.Verification;

namespace Ruth.Modules.Crm.Infrastructure.Configurations;

internal sealed class IdentityVerificationConfiguration
    : IEntityTypeConfiguration<IdentityVerification>
{
    public void Configure(EntityTypeBuilder<IdentityVerification> builder)
    {
        _ = builder.ToTable("identity_verifications");
        _ = builder.HasKey(static v => v.Id);

        _ = builder.Property(static v => v.ReferenceNumber).HasMaxLength(150).IsRequired();
        _ = builder.Property(static v => v.RequestIpAddress).HasMaxLength(45).IsRequired();

        _ = builder.HasIndex(static v => new { v.RequestIpAddress, v.IssuedOnUtc });
        _ = builder.HasIndex(static v => v.VerificationCodeId);
    }
}
