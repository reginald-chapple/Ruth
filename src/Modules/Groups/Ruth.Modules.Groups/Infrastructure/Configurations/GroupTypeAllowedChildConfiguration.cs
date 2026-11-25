using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Groups.Domain.GroupTypes;

namespace Ruth.Modules.Groups.Infrastructure.Configurations;

internal sealed class GroupTypeAllowedChildConfiguration
    : IEntityTypeConfiguration<GroupTypeAllowedChild>
{
    public void Configure(EntityTypeBuilder<GroupTypeAllowedChild> builder)
    {
        _ = builder.ToTable("group_type_allowed_children");
        _ = builder.HasKey(static c => new { c.ParentGroupTypeId, c.ChildGroupTypeId });
        _ = builder.HasIndex(static c => c.ChildGroupTypeId);
    }
}
