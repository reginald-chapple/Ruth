using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.CheckIn.Domain.Sessions;

namespace Ruth.Modules.CheckIn.Infrastructure.Configurations;

internal sealed class DailySecurityPickupCodeEntityConfiguration
    : IEntityTypeConfiguration<DailySecurityPickupCode>
{
    public void Configure(EntityTypeBuilder<DailySecurityPickupCode> builder)
    {
        _ = builder.ToTable("daily_security_pickup_codes");
        _ = builder.HasKey(static c => c.Id);
        _ = builder.Property(static c => c.Id).ValueGeneratedNever();

        _ = builder.Property(static c => c.BranchScopeKey).HasMaxLength(40).IsRequired();
        _ = builder.Property(static c => c.Code).HasMaxLength(16).IsRequired();

        _ = builder
            .HasIndex(static c => new
            {
                c.ServiceDate,
                c.BranchScopeKey,
                c.Code,
            })
            .IsUnique();
    }
}
