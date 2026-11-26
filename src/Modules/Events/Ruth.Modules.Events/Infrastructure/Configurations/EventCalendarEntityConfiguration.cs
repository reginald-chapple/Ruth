using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Events.Domain.Calendars;

namespace Ruth.Modules.Events.Infrastructure.Configurations;

internal sealed class EventCalendarEntityConfiguration : IEntityTypeConfiguration<EventCalendar>
{
    public void Configure(EntityTypeBuilder<EventCalendar> builder)
    {
        _ = builder.ToTable("event_calendars");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.Slug).HasMaxLength(96).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(1000);
        _ = builder.Property(static x => x.IconCssClass).HasMaxLength(120);
        _ = builder.Property(static x => x.ColorHex).HasMaxLength(16);

        _ = builder.Ignore(static x => x.DomainEvents);

        _ = builder.HasIndex(static x => x.Slug).IsUnique();
        _ = builder.HasIndex(static x => new { x.IsPublic, x.IsActive });
    }
}
