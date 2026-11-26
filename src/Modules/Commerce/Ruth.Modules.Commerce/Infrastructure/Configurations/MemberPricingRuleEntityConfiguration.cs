using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Commerce.Domain.Pricing;

namespace Ruth.Modules.Commerce.Infrastructure.Configurations;

internal sealed class MemberPricingRuleEntityConfiguration
    : IEntityTypeConfiguration<MemberPricingRule>
{
    public void Configure(EntityTypeBuilder<MemberPricingRule> builder)
    {
        _ = builder.ToTable("member_pricing_rules");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(1000);
        _ = builder
            .Property(static x => x.DiscountKind)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder.Property(static x => x.DiscountValue).HasPrecision(18, 2).IsRequired();
        _ = builder.Property(static x => x.RequiredMembershipTier).HasMaxLength(80);
        _ = builder
            .Property(static x => x.AppliesToProductKind)
            .HasConversion<string>()
            .HasMaxLength(32);
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => new { x.IsActive, x.Priority });
    }
}
