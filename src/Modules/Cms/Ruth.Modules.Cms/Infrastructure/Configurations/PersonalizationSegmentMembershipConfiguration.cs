using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Personalization;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class PersonalizationSegmentMembershipConfiguration
    : IEntityTypeConfiguration<PersonalizationSegmentMembership>
{
    public void Configure(EntityTypeBuilder<PersonalizationSegmentMembership> builder)
    {
        _ = builder.ToTable("personalization_segment_memberships");
        _ = builder.HasKey(static x => new { x.SegmentId, x.PersonId });
        _ = builder.HasIndex(static x => x.PersonId);
    }
}
