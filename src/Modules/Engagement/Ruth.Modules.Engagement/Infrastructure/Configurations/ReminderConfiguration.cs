using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Engagement.Domain.Reminders;

namespace Ruth.Modules.Engagement.Infrastructure.Configurations;

internal sealed class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
{
    public void Configure(EntityTypeBuilder<Reminder> builder)
    {
        _ = builder.ToTable("reminders");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.TargetEntityTypeKey).HasMaxLength(150).IsRequired();
        _ = builder.Property(static x => x.EntitySummary).HasMaxLength(250).IsRequired();
        _ = builder.Property(static x => x.Note).HasMaxLength(2000).IsRequired();
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => new { x.IsComplete, x.ReminderDate });
        _ = builder.HasIndex(static x => x.AssigneePersonId);
    }
}
