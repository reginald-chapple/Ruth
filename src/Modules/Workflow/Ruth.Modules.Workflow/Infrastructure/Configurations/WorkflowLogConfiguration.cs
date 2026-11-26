using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Workflow.Domain.Instances;

namespace Ruth.Modules.Workflow.Infrastructure.Configurations;

internal sealed class WorkflowLogConfiguration : IEntityTypeConfiguration<WorkflowLog>
{
    public void Configure(EntityTypeBuilder<WorkflowLog> builder)
    {
        _ = builder.ToTable("workflow_logs");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.EntryKind).HasConversion<string>().HasMaxLength(32);
        _ = builder.Property(static x => x.Summary).HasMaxLength(2000).IsRequired();
        _ = builder.HasIndex(static x => new { x.WorkflowId, x.LoggedAtUtc });
    }
}
