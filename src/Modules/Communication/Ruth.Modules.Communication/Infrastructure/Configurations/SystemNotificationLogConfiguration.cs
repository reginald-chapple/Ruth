using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Communication.Domain.SystemCommunications;

namespace Ruth.Modules.Communication.Infrastructure.Configurations;

internal sealed class SystemNotificationLogConfiguration
    : IEntityTypeConfiguration<SystemNotificationLog>
{
    public void Configure(EntityTypeBuilder<SystemNotificationLog> builder)
    {
        _ = builder.ToTable("system_notification_logs");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.SystemCommunicationId).IsRequired();
        _ = builder.Property(static x => x.SystemCommunicationKey).HasMaxLength(100).IsRequired();
        _ = builder
            .Property(static x => x.Channel)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        _ = builder.Property(static x => x.TransportKey).HasMaxLength(64).IsRequired();
        _ = builder.Property(static x => x.RecipientAddress).HasMaxLength(2_048).IsRequired();
        _ = builder.Property(static x => x.SubjectOrTitle).HasMaxLength(500);
        _ = builder.Property(static x => x.ProviderMessageId).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.TriggerSource).HasMaxLength(120);
        _ = builder.Property(static x => x.SentOnUtc).IsRequired();

        _ = builder.HasIndex(static x => x.SentOnUtc);
        _ = builder.HasIndex(static x => new { x.SystemCommunicationKey, x.SentOnUtc });
        _ = builder.HasIndex(static x => x.RecipientPersonId);
    }
}
