using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Finance.Domain.Statements;

namespace Ruth.Modules.Finance.Infrastructure.Configurations;

internal sealed class FinancialStatementTemplateEntityConfiguration
    : IEntityTypeConfiguration<FinancialStatementTemplate>
{
    public void Configure(EntityTypeBuilder<FinancialStatementTemplate> builder)
    {
        _ = builder.ToTable("financial_statement_templates");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder
            .Property(static x => x.StatementKind)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();
        _ = builder.Property(static x => x.OrganizationDisplayName).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.OrganizationTaxId).HasMaxLength(64);
        _ = builder.Property(static x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(500);
        _ = builder.Property(static x => x.LogoUrl).HasMaxLength(500);
        _ = builder.Property(static x => x.HeaderMergeTemplate).HasMaxLength(4000);
        _ = builder.Property(static x => x.FooterMergeTemplate).HasMaxLength(4000);
        _ = builder.Property(static x => x.CustomPrintCss).HasMaxLength(4000);
        _ = builder.Property(static x => x.MinimumTotalThreshold).HasPrecision(18, 3).IsRequired();

        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => new { x.StatementKind, x.IsActive });
    }
}
