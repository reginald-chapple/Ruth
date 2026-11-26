using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Engagement.Domain.Pipelines;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Engagement.Infrastructure.Configurations;

internal sealed class EngagementOpportunityConfiguration
    : IEntityTypeConfiguration<EngagementOpportunity>
{
    public void Configure(EntityTypeBuilder<EngagementOpportunity> builder)
    {
        _ = builder.ToTable("engagement_opportunities");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.Slug).HasMaxLength(96).IsRequired();
        _ = builder.Property(static x => x.PublicSummary).HasMaxLength(500);
        _ = builder.Property(static x => x.Description).HasMaxLength(4000);
        _ = builder.Property(static x => x.IconCssClass).HasMaxLength(100);

        EngagementJsonbConventions.ConfigureList(
            builder,
            static x => x.BranchConfigs,
            "branch_configs"
        );
        EngagementJsonbConventions.ConfigureList(
            builder,
            static x => x.PlacementGroupConfigs,
            "placement_group_configs"
        );
        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static x => x.AttributeValues,
            columnName: "attribute_values",
            createGinIndex: true
        );

        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.Slug).IsUnique();
        _ = builder.HasIndex(static x => new
        {
            x.PipelineTypeId,
            x.IsActive,
            x.IsPublic,
        });
    }
}
