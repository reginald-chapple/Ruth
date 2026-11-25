using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Groups.Domain.Groups;

namespace Ruth.Modules.Groups.Infrastructure.Configurations;

internal sealed class GroupDemographicTypeConfiguration
    : IEntityTypeConfiguration<GroupDemographicType>
{
    public void Configure(EntityTypeBuilder<GroupDemographicType> builder)
    {
        _ = builder.ToTable("group_demographic_types");
        _ = builder.HasKey(static d => d.Id);
        _ = builder.Property(static d => d.Id).ValueGeneratedNever();
        _ = builder.Property(static d => d.Name).HasMaxLength(100).IsRequired();
        _ = builder.Property(static d => d.Description).HasMaxLength(2000);
        _ = builder.Property(static d => d.ComponentKey).HasMaxLength(100).IsRequired();
        _ = builder.HasIndex(static d => new { d.GroupTypeId, d.Name }).IsUnique();
    }
}
