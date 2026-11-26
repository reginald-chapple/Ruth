using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Workflow.Domain.Forms;

namespace Ruth.Modules.Workflow.Infrastructure.Configurations;

internal sealed class WorkflowActionFormFieldConfiguration
    : IEntityTypeConfiguration<WorkflowActionFormField>
{
    public void Configure(EntityTypeBuilder<WorkflowActionFormField> builder)
    {
        _ = builder.ToTable("workflow_action_form_fields");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.SectionKey).HasMaxLength(96);
        _ = builder.Property(static x => x.AttributeKey).HasMaxLength(120).IsRequired();
        _ = builder.Property(static x => x.Label).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.FieldTypeKey).HasMaxLength(48).IsRequired();
        _ = builder.Property(static x => x.DefaultValue).HasMaxLength(2000);
        _ = builder.Property(static x => x.HelpText).HasMaxLength(500);

        WorkflowJsonbConventions.ConfigureList(
            builder,
            static x => x.SelectOptions,
            "select_options"
        );
        WorkflowJsonbConventions.ConfigureNullableObject(
            builder,
            static x => x.VisibilityRule,
            "visibility_rule"
        );

        _ = builder.HasIndex(static x => new { x.WorkflowActionFormId, x.AttributeKey }).IsUnique();
    }
}
