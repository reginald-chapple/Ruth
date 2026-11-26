using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Workflow.Domain.Definitions;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Workflow.Infrastructure.Configurations;

internal sealed class WorkflowActionTypeConfiguration : IEntityTypeConfiguration<WorkflowActionType>
{
    public void Configure(EntityTypeBuilder<WorkflowActionType> builder)
    {
        _ = builder.ToTable("workflow_action_types");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Key).HasMaxLength(96).IsRequired();
        _ = builder.Property(static x => x.Name).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.ActionPluginKey).HasMaxLength(160).IsRequired();

        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static x => x.Configuration,
            columnName: "configuration",
            createGinIndex: false
        );

        _ = builder.HasIndex(static x => new { x.WorkflowActivityTypeId, x.Key }).IsUnique();
    }
}
