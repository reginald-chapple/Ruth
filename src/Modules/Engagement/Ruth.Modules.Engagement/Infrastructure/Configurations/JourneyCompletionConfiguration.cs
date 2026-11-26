using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Engagement.Domain.Journeys;

namespace Ruth.Modules.Engagement.Infrastructure.Configurations;

internal sealed class JourneyCompletionConfiguration : IEntityTypeConfiguration<JourneyCompletion>
{
    public void Configure(EntityTypeBuilder<JourneyCompletion> builder)
    {
        _ = builder.ToTable("journey_completions");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => new { x.PersonId, x.JourneyProgramId }).IsUnique();
    }
}
