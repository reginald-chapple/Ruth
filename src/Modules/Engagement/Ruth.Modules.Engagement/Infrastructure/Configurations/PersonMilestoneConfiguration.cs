using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Engagement.Domain.Journeys;

namespace Ruth.Modules.Engagement.Infrastructure.Configurations;

internal sealed class PersonMilestoneConfiguration : IEntityTypeConfiguration<PersonMilestone>
{
    public void Configure(EntityTypeBuilder<PersonMilestone> builder)
    {
        _ = builder.ToTable("person_milestones");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Note).HasMaxLength(1000);
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => new { x.PersonId, x.JourneyProgramId });
        _ = builder.HasIndex(static x => new { x.MilestoneTypeId, x.IsCompleted });
    }
}
