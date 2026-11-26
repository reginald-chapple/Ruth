using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Workflow.Domain.Instances;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Workflow.Infrastructure.Configurations;

internal sealed class WorkflowInstanceConfiguration : IEntityTypeConfiguration<WorkflowInstance>
{
    public void Configure(EntityTypeBuilder<WorkflowInstance> builder)
    {
        _ = builder.ToTable("workflows");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.WorkflowNumber).HasMaxLength(48).IsRequired();
        _ = builder.Property(static x => x.Name).HasMaxLength(250).IsRequired();
        _ = builder.Property(static x => x.Status).HasConversion<string>().HasMaxLength(32);
        _ = builder.Property(static x => x.StateLabel).HasMaxLength(120).IsRequired();
        _ = builder.Property(static x => x.PrimaryEntityTypeKey).HasMaxLength(160);
        _ = builder.Property(static x => x.ErrorMessage).HasMaxLength(1000);

        _ = builder
            .HasMany(static x => x.Activities)
            .WithOne()
            .HasForeignKey(static a => a.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Activities)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder
            .HasMany(static x => x.Logs)
            .WithOne()
            .HasForeignKey(static l => l.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder.Navigation(static x => x.Logs).UsePropertyAccessMode(PropertyAccessMode.Field);

        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static x => x.Attributes,
            columnName: "attributes",
            createGinIndex: true
        );

        _ = builder.Ignore(static x => x.AttributeValues);
        _ = builder.Ignore(static x => x.IsTerminal);
        _ = builder.Ignore(static x => x.DomainEvents);

        _ = builder.HasIndex(static x => x.WorkflowNumber);
        _ = builder.HasIndex(static x => new { x.Status, x.NextScheduledProcessAtUtc });
        _ = builder.HasIndex(static x => x.WorkflowTypeId);
    }
}
