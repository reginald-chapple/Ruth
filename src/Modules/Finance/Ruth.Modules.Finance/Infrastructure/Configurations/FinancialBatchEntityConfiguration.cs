using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Finance.Domain.Batches;

namespace Ruth.Modules.Finance.Infrastructure.Configurations;

internal sealed class FinancialBatchEntityConfiguration : IEntityTypeConfiguration<FinancialBatch>
{
    public void Configure(EntityTypeBuilder<FinancialBatch> builder)
    {
        _ = builder.ToTable("financial_batches");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder
            .Property(static x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder.Property(static x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        _ = builder.Property(static x => x.ControlAmount).HasPrecision(18, 3).IsRequired();
        _ = builder.Property(static x => x.AccountingSystemCode).HasMaxLength(80);
        _ = builder.Property(static x => x.AuditLockNote).HasMaxLength(1000);

        _ = builder.Ignore(static x => x.DomainEvents);

        _ = builder.HasIndex(static x => new { x.Status, x.BatchStartDateTimeUtc });
    }
}
