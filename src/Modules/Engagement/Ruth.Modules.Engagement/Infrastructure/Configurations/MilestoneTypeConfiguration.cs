using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Engagement.Domain.Journeys;

namespace Ruth.Modules.Engagement.Infrastructure.Configurations;

internal sealed class MilestoneTypeConfiguration : IEntityTypeConfiguration<MilestoneType>
{
    public void Configure(EntityTypeBuilder<MilestoneType> builder)
    {
        _ = builder.ToTable("milestone_types");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(150).IsRequired();
        _ = builder.Property(static x => x.Code).HasMaxLength(64).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(1000);
        _ = builder.Property(static x => x.IconCssClass).HasMaxLength(100);

        EngagementJsonbConventions.ConfigureList(
            builder,
            static x => x.PrerequisiteMilestoneTypeIds,
            "prerequisite_milestone_type_ids"
        );

        _ = builder.HasIndex(static x => new { x.JourneyProgramId, x.Code }).IsUnique();
    }
}
