using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Finance.Domain.Transactions;

namespace Ruth.Modules.Finance.Infrastructure.Configurations;

internal sealed class FinancialTransactionRefundEntityConfiguration
    : IEntityTypeConfiguration<FinancialTransactionRefund>
{
    public void Configure(EntityTypeBuilder<FinancialTransactionRefund> builder)
    {
        _ = builder.ToTable("financial_transaction_refunds");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.TransactionId).IsRequired();
        _ = builder.Property(static x => x.Amount).HasPrecision(18, 3).IsRequired();
        _ = builder.Property(static x => x.Reason).HasMaxLength(400).IsRequired();
        _ = builder.Property(static x => x.GatewayRefundReference).HasMaxLength(120);

        _ = builder.HasIndex(static x => x.TransactionId);
    }
}
