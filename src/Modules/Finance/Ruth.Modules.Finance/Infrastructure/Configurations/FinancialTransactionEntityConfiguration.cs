using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Finance.Domain.Batches;
using Ruth.Modules.Finance.Domain.Gateways;
using Ruth.Modules.Finance.Domain.PaymentMethods;
using Ruth.Modules.Finance.Domain.Transactions;

namespace Ruth.Modules.Finance.Infrastructure.Configurations;

internal sealed class FinancialTransactionEntityConfiguration
    : IEntityTypeConfiguration<FinancialTransaction>
{
    public void Configure(EntityTypeBuilder<FinancialTransaction> builder)
    {
        _ = builder.ToTable("financial_transactions");
        _ = builder.HasKey(static x => x.Id);
        ConfigureScalars(builder);
        ConfigureNavigations(builder);
    }

    private static void ConfigureScalars(EntityTypeBuilder<FinancialTransaction> builder)
    {
        _ = builder
            .Property(static x => x.BatchId)
            .HasConversion(
                static id => id.HasValue ? id.Value.Value : (Guid?)null,
                static guid => guid.HasValue ? FinancialBatchId.From(guid.Value) : null
            );
        _ = builder
            .Property(static x => x.FinancialGatewayId)
            .HasConversion(
                static id => id.HasValue ? id.Value.Value : (Guid?)null,
                static guid => guid.HasValue ? FinancialGatewayId.From(guid.Value) : null
            );
        _ = builder
            .Property(static x => x.SavedPaymentMethodId)
            .HasConversion(
                static id => id.HasValue ? id.Value.Value : (Guid?)null,
                static guid => guid.HasValue ? SavedPaymentMethodId.From(guid.Value) : null
            );

        _ = builder.Property(static x => x.TransactionCode).HasMaxLength(80);
        _ = builder
            .Property(static x => x.SourceType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder
            .Property(static x => x.TenderType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder
            .Property(static x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder.Property(static x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        _ = builder.Property(static x => x.TotalAmount).HasPrecision(18, 3).IsRequired();
        _ = builder.Property(static x => x.RefundedAmount).HasPrecision(18, 3).IsRequired();
        _ = builder.Property(static x => x.Summary).HasMaxLength(500);
        _ = builder.Property(static x => x.CheckMicrHash).HasMaxLength(64);
    }

    private static void ConfigureNavigations(EntityTypeBuilder<FinancialTransaction> builder)
    {
        _ = builder
            .HasMany(static x => x.Details)
            .WithOne()
            .HasForeignKey(static x => x.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .HasMany(static x => x.Images)
            .WithOne()
            .HasForeignKey(static x => x.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .HasMany(static x => x.Refunds)
            .WithOne()
            .HasForeignKey(static x => x.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder
            .Navigation(static x => x.Details)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        _ = builder
            .Navigation(static x => x.Images)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        _ = builder
            .Navigation(static x => x.Refunds)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder.Ignore(static x => x.NetAmount);
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.BatchId);
        _ = builder.HasIndex(static x => new { x.AuthorizedPersonId, x.TransactionDateTimeUtc });
    }
}
