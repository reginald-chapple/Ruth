using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Engagement.Domain.Pipelines;

namespace Ruth.Modules.Engagement.Infrastructure.Configurations;

internal sealed class EngagementRequestActivityConfiguration
    : IEntityTypeConfiguration<EngagementRequestActivity>
{
    public void Configure(EntityTypeBuilder<EngagementRequestActivity> builder)
    {
        _ = builder.ToTable("engagement_request_activities");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.ActivityName).HasMaxLength(150).IsRequired();
        _ = builder.Property(static x => x.Note).HasMaxLength(1000);
        _ = builder.HasIndex(static x => new { x.EngagementRequestId, x.OccurredAtUtc });
    }
}
