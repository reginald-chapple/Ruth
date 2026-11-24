using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ruth.Persistence.Outbox.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        _ = builder.ToTable(OutboxSqlConstants.OutboxTable);
        _ = builder.HasKey(x => x.Id);

        ConfigurePayloadProperties(builder);
        ConfigureDeliveryProperties(builder);

        _ = builder
            .HasIndex(x => new { x.ProcessedOnUtc, x.NextRetryOnUtc })
            .HasFilter(
                $"\"{OutboxSqlConstants.ColumnProcessedOnUtc}\" IS NULL AND \"{OutboxSqlConstants.ColumnDeadLetteredOnUtc}\" IS NULL"
            );
    }

    private static void ConfigurePayloadProperties(EntityTypeBuilder<OutboxMessage> builder)
    {
        _ = builder
            .Property(x => x.Id)
            .HasColumnName(OutboxSqlConstants.ColumnId)
            .ValueGeneratedNever();

        _ = builder
            .Property(x => x.EventType)
            .HasColumnName(OutboxSqlConstants.ColumnEventType)
            .HasMaxLength(512)
            .IsRequired();

        _ = builder
            .Property(x => x.Payload)
            .HasColumnName(OutboxSqlConstants.ColumnPayload)
            .HasColumnType("jsonb")
            .IsRequired();

        _ = builder
            .Property(x => x.OccurredOnUtc)
            .HasColumnName(OutboxSqlConstants.ColumnOccurredOnUtc)
            .IsRequired();
    }

    private static void ConfigureDeliveryProperties(EntityTypeBuilder<OutboxMessage> builder)
    {
        _ = builder
            .Property(x => x.ProcessedOnUtc)
            .HasColumnName(OutboxSqlConstants.ColumnProcessedOnUtc);

        _ = builder
            .Property(x => x.DeadLetteredOnUtc)
            .HasColumnName(OutboxSqlConstants.ColumnDeadLetteredOnUtc);

        _ = builder
            .Property(x => x.RetryCount)
            .HasColumnName(OutboxSqlConstants.ColumnRetryCount)
            .HasDefaultValue(0)
            .IsRequired();

        _ = builder
            .Property(x => x.NextRetryOnUtc)
            .HasColumnName(OutboxSqlConstants.ColumnNextRetryOnUtc);

        _ = builder
            .Property(x => x.LockedUntilUtc)
            .HasColumnName(OutboxSqlConstants.ColumnLockedUntilUtc);

        _ = builder.Property(x => x.LockToken).HasColumnName(OutboxSqlConstants.ColumnLockToken);

        _ = builder
            .Property(x => x.Error)
            .HasColumnName(OutboxSqlConstants.ColumnError)
            .HasMaxLength(2048);
    }
}
