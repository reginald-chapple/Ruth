using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Workflow.Domain.Forms;

namespace Ruth.Modules.Workflow.Infrastructure.Configurations;

internal sealed class WorkflowActionFormConfiguration : IEntityTypeConfiguration<WorkflowActionForm>
{
    public void Configure(EntityTypeBuilder<WorkflowActionForm> builder)
    {
        _ = builder.ToTable("workflow_action_forms");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.HeaderMarkup).HasMaxLength(8000);
        _ = builder.Property(static x => x.FooterMarkup).HasMaxLength(8000);
        _ = builder.Property(static x => x.NotificationSystemCommunicationKey).HasMaxLength(160);

        _ = builder
            .HasMany(static x => x.Sections)
            .WithOne()
            .HasForeignKey(static s => s.WorkflowActionFormId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Sections)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder
            .HasMany(static x => x.Fields)
            .WithOne()
            .HasForeignKey(static f => f.WorkflowActionFormId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Fields)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        WorkflowJsonbConventions.ConfigureList(builder, static x => x.Buttons, "buttons");

        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.WorkflowActionTypeId).IsUnique();
    }
}
