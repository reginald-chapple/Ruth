using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Groups.Domain.Scheduling;

namespace Ruth.Modules.Groups.Infrastructure.Configurations;

internal sealed class GroupMemberScheduleTemplateConfiguration
    : IEntityTypeConfiguration<GroupMemberScheduleTemplate>
{
    public void Configure(EntityTypeBuilder<GroupMemberScheduleTemplate> builder)
    {
        _ = builder.ToTable("group_member_schedule_templates");
        _ = builder.HasKey(static t => t.Id);
        _ = builder.Property(static t => t.Id).ValueGeneratedNever();
        _ = builder.Property(static t => t.Name).HasMaxLength(100).IsRequired();
        _ = builder.Property(static t => t.RecurrenceDescription).HasMaxLength(150).IsRequired();
        _ = builder.HasIndex(static t => t.GroupTypeId);
    }
}
