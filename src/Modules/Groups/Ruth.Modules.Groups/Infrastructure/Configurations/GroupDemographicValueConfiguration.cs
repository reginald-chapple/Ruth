using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Groups.Domain.Groups;

namespace Ruth.Modules.Groups.Infrastructure.Configurations;

internal sealed class GroupDemographicValueConfiguration
    : IEntityTypeConfiguration<GroupDemographicValue>
{
    public void Configure(EntityTypeBuilder<GroupDemographicValue> builder)
    {
        _ = builder.ToTable("group_demographic_values");
        _ = builder.HasKey(static v => v.Id);
        _ = builder.Property(static v => v.Id).ValueGeneratedNever();
        _ = builder.Property(static v => v.Value).HasMaxLength(2000);
        _ = builder.Property(static v => v.ValueAsNumeric).HasPrecision(18, 2);
        _ = builder.Property(static v => v.RelatedEntityTypeKey).HasMaxLength(100);
        _ = builder.HasIndex(static v => new { v.GroupId, v.GroupDemographicTypeId }).IsUnique();
    }
}
