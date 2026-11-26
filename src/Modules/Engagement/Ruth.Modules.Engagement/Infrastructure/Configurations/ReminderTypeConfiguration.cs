using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Engagement.Domain.Reminders;

namespace Ruth.Modules.Engagement.Infrastructure.Configurations;

internal sealed class ReminderTypeConfiguration : IEntityTypeConfiguration<ReminderType>
{
    public void Configure(EntityTypeBuilder<ReminderType> builder)
    {
        _ = builder.ToTable("reminder_types");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(150).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(500);
        _ = builder.Property(static x => x.EntityTypeKeyFilter).HasMaxLength(150);
        _ = builder.Property(static x => x.HighlightColorHex).HasMaxLength(16).IsRequired();
        _ = builder
            .Property(static x => x.NotificationFlavor)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder.Property(static x => x.SystemCommunicationKey).HasMaxLength(120);
        _ = builder.Ignore(static x => x.DomainEvents);
    }
}
