using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Finance.Domain.Statements;

namespace Ruth.Modules.Finance.Infrastructure.Configurations;

internal sealed class GeneratedFinancialStatementEntityConfiguration
    : IEntityTypeConfiguration<GeneratedFinancialStatement>
{
    public void Configure(EntityTypeBuilder<GeneratedFinancialStatement> builder)
    {
        _ = builder.ToTable("generated_financial_statements");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.StatementNumber).HasMaxLength(40).IsRequired();
        _ = builder.Property(static x => x.TemplateId).IsRequired();
        _ = builder
            .Property(static x => x.StatementKind)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();
        _ = builder
            .Property(static x => x.RecipientScope)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder.Property(static x => x.RecipientDisplayName).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        _ = builder.Property(static x => x.TaxDeductibleTotal).HasPrecision(18, 3).IsRequired();
        _ = builder.Property(static x => x.NonDeductiblePaidTotal).HasPrecision(18, 3).IsRequired();
        _ = builder
            .Property(static x => x.OpenInvoiceBalanceTotal)
            .HasPrecision(18, 3)
            .IsRequired();
        _ = builder.Property(static x => x.HtmlStorageKey).HasMaxLength(260);
        _ = builder.Property(static x => x.PdfStorageKey).HasMaxLength(260);

        _ = builder.Ignore(static x => x.DomainEvents);

        _ = builder.HasIndex(static x => x.StatementNumber).IsUnique();
        _ = builder.HasIndex(static x => x.TargetPersonId);
        _ = builder.HasIndex(static x => x.TargetHouseholdId);
        _ = builder.HasIndex(static x => x.BatchRunId);
    }
}
