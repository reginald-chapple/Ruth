using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Finance.Domain.Invoices;
using Ruth.Modules.Finance.Domain.Transactions;

namespace Ruth.Modules.Finance.Infrastructure.Configurations;

internal sealed class FinancialTransactionDetailEntityConfiguration
    : IEntityTypeConfiguration<FinancialTransactionDetail>
{
    public void Configure(EntityTypeBuilder<FinancialTransactionDetail> builder)
    {
        _ = builder.ToTable("financial_transaction_details");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.TransactionId).IsRequired();
        _ = builder.Property(static x => x.AccountId).IsRequired();
        _ = builder.Property(static x => x.Amount).HasPrecision(18, 3).IsRequired();
        _ = builder
            .Property(static x => x.InvoiceId)
            .HasConversion(
                static id => id.HasValue ? id.Value.Value : (Guid?)null,
                static guid => guid.HasValue ? InvoiceId.From(guid.Value) : null
            );
        _ = builder.Property(static x => x.Summary).HasMaxLength(300);

        _ = builder.HasIndex(static x => x.AccountId);
        _ = builder.HasIndex(static x => x.InvoiceId);
    }
}
