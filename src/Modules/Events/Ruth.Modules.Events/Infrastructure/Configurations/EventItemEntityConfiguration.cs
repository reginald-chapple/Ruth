using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Events.Domain.Calendars;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Events.Infrastructure.Configurations;

internal sealed class EventItemEntityConfiguration : IEntityTypeConfiguration<EventItem>
{
    public void Configure(EntityTypeBuilder<EventItem> builder)
    {
        _ = builder.ToTable("event_items");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.Name).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.Slug).HasMaxLength(96).IsRequired();
        _ = builder.Property(static x => x.Summary).HasMaxLength(500);
        _ = builder.Property(static x => x.Description).HasMaxLength(4000);
        _ = builder.Property(static x => x.DetailsUrl).HasMaxLength(500);
        _ = builder
            .Property(static x => x.ApprovalStatus)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static x => x.AttributeValues,
            columnName: "attribute_values",
            createGinIndex: true
        );

        _ = builder
            .HasMany(static x => x.CalendarItems)
            .WithOne()
            .HasForeignKey(static x => x.EventItemId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.CalendarItems)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder
            .HasMany(static x => x.Audiences)
            .WithOne()
            .HasForeignKey(static x => x.EventItemId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Audiences)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder
            .HasMany(static x => x.Occurrences)
            .WithOne()
            .HasForeignKey(static x => x.EventItemId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Occurrences)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder.Ignore(static x => x.DomainEvents);

        _ = builder.HasIndex(static x => x.Slug).IsUnique();
        _ = builder.HasIndex(static x => new
        {
            x.ApprovalStatus,
            x.IsActive,
            x.IsPublic,
        });
    }
}
