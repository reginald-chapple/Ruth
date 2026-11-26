using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Workflow.Domain.Definitions;

namespace Ruth.Modules.Workflow.Infrastructure.Configurations;

internal sealed class WorkflowActivityTypeConfiguration
    : IEntityTypeConfiguration<WorkflowActivityType>
{
    public void Configure(EntityTypeBuilder<WorkflowActivityType> builder)
    {
        _ = builder.ToTable("workflow_activity_types");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Key).HasMaxLength(96).IsRequired();
        _ = builder.Property(static x => x.Name).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(2000);

        _ = builder
            .HasMany(static x => x.Actions)
            .WithOne()
            .HasForeignKey(static a => a.WorkflowActivityTypeId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Actions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder.HasIndex(static x => new { x.WorkflowTypeId, x.Key }).IsUnique();
    }
}
