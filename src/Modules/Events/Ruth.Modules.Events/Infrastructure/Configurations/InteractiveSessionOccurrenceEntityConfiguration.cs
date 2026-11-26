using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Events.Domain.InteractiveSessions;

namespace Ruth.Modules.Events.Infrastructure.Configurations;

internal sealed class InteractiveSessionOccurrenceEntityConfiguration
    : IEntityTypeConfiguration<InteractiveSessionOccurrence>
{
    public void Configure(EntityTypeBuilder<InteractiveSessionOccurrence> builder)
    {
        _ = builder.ToTable("interactive_session_occurrences");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.SessionId).IsRequired();
        _ = builder.HasIndex(static x => x.SessionId);
    }
}
