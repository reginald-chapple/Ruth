using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Lms.Domain.Grading;

namespace Ruth.Modules.Lms.Infrastructure.Configurations;

internal sealed class LearningGradingScaleConfiguration
    : IEntityTypeConfiguration<LearningGradingScale>
{
    public void Configure(EntityTypeBuilder<LearningGradingScale> builder)
    {
        _ = builder.ToTable("learning_grading_scales");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(64).IsRequired();
        _ = builder.Property(static x => x.ThresholdPercentage).HasPrecision(5, 2);
        _ = builder.Property(static x => x.HighlightColor).HasMaxLength(32);
        _ = builder.Property(static x => x.Description).HasMaxLength(500);
        _ = builder.HasIndex(static x => new { x.LearningGradingSystemId, x.Order });
    }
}
