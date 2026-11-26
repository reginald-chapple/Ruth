using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Engagement.Domain.Pipelines;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Engagement.Infrastructure.Configurations;

internal sealed class PipelineTypeConfiguration : IEntityTypeConfiguration<PipelineType>
{
    public void Configure(EntityTypeBuilder<PipelineType> builder)
    {
        _ = builder.ToTable("pipeline_types");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.Slug).HasMaxLength(96).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(2000);

        _ = builder
            .HasMany(static x => x.Stages)
            .WithOne()
            .HasForeignKey(static s => s.PipelineTypeId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Stages)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder
            .HasMany(static x => x.ActivityTypes)
            .WithOne()
            .HasForeignKey(static a => a.PipelineTypeId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.ActivityTypes)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

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
