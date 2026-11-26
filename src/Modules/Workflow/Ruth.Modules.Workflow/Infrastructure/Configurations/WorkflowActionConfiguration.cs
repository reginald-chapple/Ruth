using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Workflow.Domain.Instances;

namespace Ruth.Modules.Workflow.Infrastructure.Configurations;

internal sealed class WorkflowActionConfiguration : IEntityTypeConfiguration<WorkflowAction>
{
    public void Configure(EntityTypeBuilder<WorkflowAction> builder)
    {
        _ = builder.ToTable("workflow_actions");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Key).HasMaxLength(96).IsRequired();
        _ = builder.Property(static x => x.Name).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.ActionPluginKey).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.FormActionSelected).HasMaxLength(96);

        _ = builder.Ignore(static x => x.IsCompleted);
        _ = builder.HasIndex(static x => x.WorkflowActivityId);
    }
}
