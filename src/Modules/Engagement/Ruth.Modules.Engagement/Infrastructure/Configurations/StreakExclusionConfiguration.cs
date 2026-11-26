using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Engagement.Domain.Streaks;

namespace Ruth.Modules.Engagement.Infrastructure.Configurations;

internal sealed class StreakExclusionConfiguration : IEntityTypeConfiguration<StreakExclusion>
{
    public void Configure(EntityTypeBuilder<StreakExclusion> builder)
    {
        _ = builder.ToTable("streak_exclusions");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.HasIndex(static x => new { x.StreakTypeId, x.ExclusionDate });
    }
}
