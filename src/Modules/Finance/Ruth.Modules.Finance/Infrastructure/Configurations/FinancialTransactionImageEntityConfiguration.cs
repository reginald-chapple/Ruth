using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Finance.Domain.Transactions;

namespace Ruth.Modules.Finance.Infrastructure.Configurations;

internal sealed class FinancialTransactionImageEntityConfiguration
    : IEntityTypeConfiguration<FinancialTransactionImage>
{
    public void Configure(EntityTypeBuilder<FinancialTransactionImage> builder)
    {
        _ = builder.ToTable("financial_transaction_images");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.TransactionId).IsRequired();
        _ = builder
            .Property(static x => x.ImageSide)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();
        _ = builder.Property(static x => x.StorageObjectKey).HasMaxLength(300).IsRequired();
        _ = builder.Property(static x => x.ContentType).HasMaxLength(80).IsRequired();
        _ = builder.Property(static x => x.Sha256PlaintextHex).HasMaxLength(64).IsRequired();
        _ = builder.Property(static x => x.EncryptionNonceBase64).HasMaxLength(32).IsRequired();
        _ = builder.Property(static x => x.EncryptionTagBase64).HasMaxLength(32).IsRequired();
        _ = builder.Property(static x => x.KeyIdentifier).HasMaxLength(64).IsRequired();

        _ = builder.HasIndex(static x => x.TransactionId);
    }
}
