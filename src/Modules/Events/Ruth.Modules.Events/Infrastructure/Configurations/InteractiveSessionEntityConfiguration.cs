using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Events.Domain.InteractiveSessions;

namespace Ruth.Modules.Events.Infrastructure.Configurations;

internal sealed class InteractiveSessionEntityConfiguration
    : IEntityTypeConfiguration<InteractiveSession>
{
    public void Configure(EntityTypeBuilder<InteractiveSession> builder)
    {
        _ = builder.ToTable("interactive_sessions");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.Title).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.JoinCode).HasMaxLength(12).IsRequired();
        _ = builder
            .Property(static x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();
        _ = builder
            .Property(static x => x.ActiveActionId)
            .HasConversion(
                static id => id.HasValue ? id.Value.Value : (Guid?)null,
                static g => g.HasValue ? InteractiveActionId.From(g.Value) : null
            );
        _ = builder
            .Property(static x => x.ActiveOccurrenceId)
            .HasConversion(
                static id => id.HasValue ? id.Value.Value : (Guid?)null,
                static g => g.HasValue ? InteractiveSessionOccurrenceId.From(g.Value) : null
            );

        _ = builder
            .HasMany(static x => x.Actions)
            .WithOne()
            .HasForeignKey(static x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Actions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder
            .HasMany(static x => x.Occurrences)
            .WithOne()
            .HasForeignKey(static x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Occurrences)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder
            .HasMany(static x => x.Responses)
            .WithOne()
            .HasForeignKey(static x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Responses)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder.Ignore(static x => x.DomainEvents);

        _ = builder.HasIndex(static x => x.JoinCode).IsUnique();
        _ = builder.HasIndex(static x => x.Status);
    }
}
