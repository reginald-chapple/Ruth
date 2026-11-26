using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Engagement.Domain.Journeys;

namespace Ruth.Modules.Engagement.Infrastructure.Configurations;

internal sealed class MilestoneStatusConfiguration : IEntityTypeConfiguration<MilestoneStatus>
{
    public void Configure(EntityTypeBuilder<MilestoneStatus> builder)
    {
        _ = builder.ToTable("milestone_statuses");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(100).IsRequired();
        _ = builder.Property(static x => x.ColorHex).HasMaxLength(16).IsRequired();
        _ = builder.HasIndex(static x => new { x.JourneyProgramId, x.Order });
    }
}
