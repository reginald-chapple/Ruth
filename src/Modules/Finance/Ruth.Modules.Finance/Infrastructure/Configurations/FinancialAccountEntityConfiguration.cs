using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Finance.Infrastructure.Configurations;

internal sealed class FinancialAccountEntityConfiguration
    : IEntityTypeConfiguration<FinancialAccount>
{
    public void Configure(EntityTypeBuilder<FinancialAccount> builder)
    {
        _ = builder.ToTable("financial_accounts");
        _ = builder.HasKey(static x => x.Id);

        _ = builder
            .Property(static x => x.ParentAccountId)
            .HasConversion(
                static id => id.HasValue ? id.Value.Value : (Guid?)null,
                static guid => guid.HasValue ? FinancialAccountId.From(guid.Value) : null
            );

        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.PublicName).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.Code).HasMaxLength(64).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(500);
        _ = builder
            .Property(static x => x.AccountKind)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();
        _ = builder.Property(static x => x.DefaultCurrencyCode).HasMaxLength(3).IsRequired();
        _ = builder.Property(static x => x.GlCode).HasMaxLength(64);
        _ = builder.Property(static x => x.CostCenterCode).HasMaxLength(64);

        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static x => x.AttributeValues,
            columnName: "attribute_values",
            createGinIndex: true
        );

        _ = builder.Ignore(static x => x.DomainEvents);

        _ = builder.HasIndex(static x => x.Code).IsUnique();
        _ = builder.HasIndex(static x => x.ParentAccountId);
        _ = builder.HasIndex(static x => new { x.AccountKind, x.IsActive });
    }
}
