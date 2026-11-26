using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Engagement.Domain.Pipelines;

namespace Ruth.Modules.Engagement.Infrastructure.Configurations;

internal sealed class PipelineStageConfiguration : IEntityTypeConfiguration<PipelineStage>
{
    public void Configure(EntityTypeBuilder<PipelineStage> builder)
    {
        _ = builder.ToTable("pipeline_stages");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(150).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(1000);

        EngagementJsonbConventions.ConfigureList(builder, static x => x.Automations, "automations");

        _ = builder.HasIndex(static x => new { x.PipelineTypeId, x.Order });
    }
}
