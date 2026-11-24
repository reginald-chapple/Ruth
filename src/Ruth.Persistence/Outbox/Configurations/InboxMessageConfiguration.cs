using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ruth.Persistence.Outbox.Configurations;

internal sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        _ = builder.ToTable(OutboxSqlConstants.InboxTable);

        _ = builder.HasKey(x => new { x.MessageId, x.ConsumerName });

        _ = builder
            .Property(x => x.MessageId)
            .HasColumnName(OutboxSqlConstants.ColumnMessageId)
            .ValueGeneratedNever();

        _ = builder
            .Property(x => x.ConsumerName)
            .HasColumnName(OutboxSqlConstants.ColumnConsumerName)
            .HasMaxLength(256)
            .IsRequired();

        _ = builder
            .Property(x => x.ProcessedOnUtc)
            .HasColumnName(OutboxSqlConstants.ColumnProcessedOnUtc)
            .IsRequired();
    }
}
