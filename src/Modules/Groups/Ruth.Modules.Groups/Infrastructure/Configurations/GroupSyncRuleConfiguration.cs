using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Groups.Domain.Sync;

namespace Ruth.Modules.Groups.Infrastructure.Configurations;

internal sealed class GroupSyncRuleConfiguration : IEntityTypeConfiguration<GroupSyncRule>
{
    public void Configure(EntityTypeBuilder<GroupSyncRule> builder)
    {
        _ = builder.ToTable("group_sync_rules");
        _ = builder.HasKey(static s => s.Id);
        _ = builder.Property(static s => s.Id).ValueGeneratedNever();
        _ = builder.Property(static s => s.SourceType).HasConversion<string>().HasMaxLength(32);
        _ = builder
            .Property(static s => s.TargetMemberStatus)
            .HasConversion<string>()
            .HasMaxLength(16);
        _ = builder.Property(static s => s.SourceFilterValue).HasMaxLength(150);
        _ = builder.HasIndex(static s => new { s.GroupId, s.GroupTypeRoleId }).IsUnique();
    }
}
