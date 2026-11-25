using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Groups.Domain.Members;

namespace Ruth.Modules.Groups.Infrastructure.Configurations;

internal sealed class GroupMemberHistoricalConfiguration
    : IEntityTypeConfiguration<GroupMemberHistorical>
{
    public void Configure(EntityTypeBuilder<GroupMemberHistorical> builder)
    {
        _ = builder.ToTable("group_member_historicals");
        _ = builder.HasKey(static h => h.Id);
        _ = builder.Property(static h => h.Id).ValueGeneratedNever();
        _ = builder.Property(static h => h.GroupRoleName).HasMaxLength(100).IsRequired();
        _ = builder.Property(static h => h.Status).HasConversion<string>().HasMaxLength(16);
        _ = builder.HasIndex(static h => new { h.GroupMemberId, h.CurrentRowIndicator });
        _ = builder.HasIndex(static h => h.PersonId);
    }
}
