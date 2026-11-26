using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Events.Domain.InteractiveSessions;

namespace Ruth.Modules.Events.Infrastructure.Configurations;

internal sealed class InteractiveActionEntityConfiguration
    : IEntityTypeConfiguration<InteractiveAction>
{
    public void Configure(EntityTypeBuilder<InteractiveAction> builder)
    {
        _ = builder.ToTable("interactive_actions");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.SessionId).IsRequired();
        _ = builder.Property(static x => x.Title).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.PromptText).HasMaxLength(1000).IsRequired();
        _ = builder
            .Property(static x => x.ActionType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        EventsJsonbListConventions.ConfigureStringList(
            builder,
            static x => x.Options,
            columnName: "options"
        );

        _ = builder.HasIndex(static x => new { x.SessionId, x.DisplayOrder });
    }
}
