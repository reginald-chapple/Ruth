using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Crm.Domain.Verification;

namespace Ruth.Modules.Crm.Infrastructure.Configurations;

internal sealed class IdentityVerificationCodeConfiguration
    : IEntityTypeConfiguration<IdentityVerificationCode>
{
    public void Configure(EntityTypeBuilder<IdentityVerificationCode> builder)
    {
        _ = builder.ToTable("identity_verification_codes");
        _ = builder.HasKey(static c => c.Id);

        _ = builder.Property(static c => c.Code).HasMaxLength(6).IsRequired();
        _ = builder.HasIndex(static c => c.Code).IsUnique();
    }
}
