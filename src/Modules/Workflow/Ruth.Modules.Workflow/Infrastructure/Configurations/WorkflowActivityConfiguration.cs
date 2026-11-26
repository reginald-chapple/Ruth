using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Workflow.Domain.Instances;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Workflow.Infrastructure.Configurations;

internal sealed class WorkflowActivityConfiguration : IEntityTypeConfiguration<WorkflowActivity>
{
    public void Configure(EntityTypeBuilder<WorkflowActivity> builder)
    {
        _ = builder.ToTable("workflow_activities");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Key).HasMaxLength(96).IsRequired();
        _ = builder.Property(static x => x.Name).HasMaxLength(200).IsRequired();

        _ = builder
            .HasMany(static x => x.Actions)
            .WithOne()
            .HasForeignKey(static a => a.WorkflowActivityId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Actions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static x => x.Attributes,
            columnName: "attributes",
            createGinIndex: false
        );

        _ = builder.Ignore(static x => x.IsCompleted);
        _ = builder.HasIndex(static x => new { x.AssignedPersonId, x.CompletedAtUtc });
        _ = builder.HasIndex(static x => new { x.AssignedGroupId, x.CompletedAtUtc });
    }
}
