using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Engagement.Domain.Reminders;

namespace Ruth.Modules.Engagement.Infrastructure.Configurations;

internal sealed class NotificationMessageConfiguration
    : IEntityTypeConfiguration<NotificationMessage>
{
    public void Configure(EntityTypeBuilder<NotificationMessage> builder)
    {
        _ = builder.ToTable("notification_messages");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Title).HasMaxLength(250).IsRequired();
        _ = builder.Property(static x => x.Message).HasMaxLength(2000).IsRequired();
        _ = builder
            .Property(static x => x.Category)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder.Property(static x => x.ActionUrl).HasMaxLength(500);
        _ = builder.Property(static x => x.ColorHex).HasMaxLength(16);
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => new
        {
            x.RecipientPersonId,
            x.IsRead,
            x.CreatedOnUtc,
        });
    }
}
