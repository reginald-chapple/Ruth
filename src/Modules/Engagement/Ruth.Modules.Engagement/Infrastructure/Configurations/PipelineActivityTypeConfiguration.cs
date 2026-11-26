using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Engagement.Domain.Pipelines;

namespace Ruth.Modules.Engagement.Infrastructure.Configurations;

internal sealed class PipelineActivityTypeConfiguration
    : IEntityTypeConfiguration<PipelineActivityType>
{
    public void Configure(EntityTypeBuilder<PipelineActivityType> builder)
    {
        _ = builder.ToTable("pipeline_activity_types");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(150).IsRequired();
        _ = builder.HasIndex(static x => new { x.PipelineTypeId, x.Order });
    }
}
