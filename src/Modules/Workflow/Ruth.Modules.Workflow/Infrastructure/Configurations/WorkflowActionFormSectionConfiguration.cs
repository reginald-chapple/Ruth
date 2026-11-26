using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Workflow.Domain.Forms;

namespace Ruth.Modules.Workflow.Infrastructure.Configurations;

internal sealed class WorkflowActionFormSectionConfiguration
    : IEntityTypeConfiguration<WorkflowActionFormSection>
{
    public void Configure(EntityTypeBuilder<WorkflowActionFormSection> builder)
    {
        _ = builder.ToTable("workflow_action_form_sections");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.SectionKey).HasMaxLength(96).IsRequired();
        _ = builder.Property(static x => x.Title).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(1000);

        WorkflowJsonbConventions.ConfigureNullableObject(
            builder,
            static x => x.VisibilityRule,
            "visibility_rule"
        );

        _ = builder.HasIndex(static x => new { x.WorkflowActionFormId, x.SectionKey }).IsUnique();
    }
}
