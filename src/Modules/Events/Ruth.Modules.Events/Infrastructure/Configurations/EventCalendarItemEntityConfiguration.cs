using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Events.Domain.Calendars;

namespace Ruth.Modules.Events.Infrastructure.Configurations;

internal sealed class EventCalendarItemEntityConfiguration
    : IEntityTypeConfiguration<EventCalendarItem>
{
    public void Configure(EntityTypeBuilder<EventCalendarItem> builder)
    {
        _ = builder.ToTable("event_calendar_items");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.EventItemId).IsRequired();
        _ = builder.Property(static x => x.CalendarId).IsRequired();

        _ = builder.HasIndex(static x => new { x.CalendarId, x.EventItemId }).IsUnique();
    }
}
