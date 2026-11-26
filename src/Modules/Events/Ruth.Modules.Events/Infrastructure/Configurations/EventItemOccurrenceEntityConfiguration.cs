using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Events.Domain.Calendars;

namespace Ruth.Modules.Events.Infrastructure.Configurations;

internal sealed class EventItemOccurrenceEntityConfiguration
    : IEntityTypeConfiguration<EventItemOccurrence>
{
    public void Configure(EntityTypeBuilder<EventItemOccurrence> builder)
    {
        _ = builder.ToTable("event_item_occurrences");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.EventItemId).IsRequired();
        _ = builder.Property(static x => x.LocationDescription).HasMaxLength(240);
        _ = builder.Property(static x => x.TimeZoneId).HasMaxLength(80);
        _ = builder.Property(static x => x.ContactPhone).HasMaxLength(40);
        _ = builder.Property(static x => x.ContactEmail).HasMaxLength(254);
        _ = builder.Property(static x => x.OccurrenceNote).HasMaxLength(1000);

        _ = builder.HasIndex(static x => x.EventItemId);
        _ = builder.HasIndex(static x => x.BranchId);
        _ = builder.HasIndex(static x => x.ScheduleId);
        _ = builder.HasIndex(static x => x.ContactPersonId);
    }
}
