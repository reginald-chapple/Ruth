using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Events.Domain.InteractiveSessions;

namespace Ruth.Modules.Events.Infrastructure.Configurations;

internal sealed class InteractiveResponseEntityConfiguration
    : IEntityTypeConfiguration<InteractiveResponse>
{
    public void Configure(EntityTypeBuilder<InteractiveResponse> builder)
    {
        _ = builder.ToTable("interactive_responses");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.SessionId).IsRequired();
        _ = builder.Property(static x => x.ActionId).IsRequired();
        _ = builder
            .Property(static x => x.OccurrenceId)
            .HasConversion(
                static id => id.HasValue ? id.Value.Value : (Guid?)null,
                static g => g.HasValue ? InteractiveSessionOccurrenceId.From(g.Value) : null
            );
        _ = builder.Property(static x => x.ParticipantToken).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.SelectedOption).HasMaxLength(240);
        _ = builder.Property(static x => x.TextResponse).HasMaxLength(1000);
        _ = builder
            .Property(static x => x.ModerationStatus)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();

        _ = builder.HasIndex(static x => new { x.SessionId, x.ActionId });
        _ = builder.HasIndex(static x => x.ResponderPersonId);
    }
}
