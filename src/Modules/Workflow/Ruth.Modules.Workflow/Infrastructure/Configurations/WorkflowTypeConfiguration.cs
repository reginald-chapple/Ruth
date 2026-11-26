using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Workflow.Domain.Definitions;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Workflow.Infrastructure.Configurations;

internal sealed class WorkflowTypeConfiguration : IEntityTypeConfiguration<WorkflowType>
{
    public void Configure(EntityTypeBuilder<WorkflowType> builder)
    {
        _ = builder.ToTable("workflow_types");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.Slug).HasMaxLength(96).IsRequired();
        _ = builder.Property(static x => x.WorkTerm).HasMaxLength(80).IsRequired();
        _ = builder.Property(static x => x.WorkflowIdPrefix).HasMaxLength(24).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(2000);
        _ = builder.Property(static x => x.LoggingLevel).HasConversion<string>().HasMaxLength(32);

        _ = builder
            .HasMany(static x => x.Activities)
            .WithOne()
            .HasForeignKey(static a => a.WorkflowTypeId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Activities)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        WorkflowJsonbConventions.ConfigureList(
            builder,
            static x => x.DeclaredAttributes,
            "declared_attributes"
        );
        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static x => x.AttributeValues,
            columnName: "attribute_values",
            createGinIndex: true
        );

        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.Slug).IsUnique();
    }
}
