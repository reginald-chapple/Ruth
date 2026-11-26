using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Events.Domain.Calendars;

namespace Ruth.Modules.Events.Infrastructure.Configurations;

internal sealed class EventItemAudienceEntityConfiguration
    : IEntityTypeConfiguration<EventItemAudience>
{
    public void Configure(EntityTypeBuilder<EventItemAudience> builder)
    {
        _ = builder.ToTable("event_item_audiences");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.EventItemId).IsRequired();
        _ = builder.Property(static x => x.AudienceLabel).HasMaxLength(120).IsRequired();

        _ = builder.HasIndex(static x => x.EventItemId);
        _ = builder.HasIndex(static x => x.AudienceDefinedValueId);
    }
}
