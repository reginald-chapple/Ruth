using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Crm.Domain.Households;

namespace Ruth.Modules.Crm.Infrastructure.Configurations;

internal sealed class HouseholdMemberConfiguration : IEntityTypeConfiguration<HouseholdMember>
{
    public void Configure(EntityTypeBuilder<HouseholdMember> builder)
    {
        _ = builder.ToTable("household_members");
        _ = builder.HasKey(static m => m.Id);

        _ = builder.Property(static m => m.Role).HasConversion<string>().HasMaxLength(16);

        _ = builder.HasIndex(static m => new { m.HouseholdId, m.PersonId }).IsUnique();
        _ = builder.HasIndex(static m => new { m.PersonId, m.HouseholdOrder });
    }
}
