using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Workflow.Domain.Triggers;

namespace Ruth.Modules.Workflow.Infrastructure.Configurations;

internal sealed class WorkflowTriggerConfiguration : IEntityTypeConfiguration<WorkflowTrigger>
{
    public void Configure(EntityTypeBuilder<WorkflowTrigger> builder)
    {
        _ = builder.ToTable("workflow_triggers");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.TriggerKind).HasConversion<string>().HasMaxLength(32);
        _ = builder.Property(static x => x.EventTypeKey).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.EntityTypeQualifier).HasMaxLength(160);
        _ = builder.Property(static x => x.FilterAttributeKey).HasMaxLength(120);
        _ = builder.Property(static x => x.FilterOperator).HasConversion<string>().HasMaxLength(32);
        _ = builder.Property(static x => x.FilterExpectedValue).HasMaxLength(400);
        _ = builder.Property(static x => x.WorkflowNameTemplate).HasMaxLength(250);

        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => new
        {
            x.IsActive,
            x.TriggerKind,
            x.EventTypeKey,
        });
    }
}
