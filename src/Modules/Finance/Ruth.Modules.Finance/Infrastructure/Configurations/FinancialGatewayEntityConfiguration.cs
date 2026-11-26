using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Finance.Domain.Gateways;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Finance.Infrastructure.Configurations;

internal sealed class FinancialGatewayEntityConfiguration
    : IEntityTypeConfiguration<FinancialGateway>
{
    public void Configure(EntityTypeBuilder<FinancialGateway> builder)
    {
        _ = builder.ToTable("financial_gateways");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.Name).HasMaxLength(120).IsRequired();
        _ = builder.Property(static x => x.ProviderKey).HasMaxLength(64).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(400);
        _ = builder.Property(static x => x.PublicTokenizerKey).HasMaxLength(200);

        JsonbAttributeConventions.ConfigureJsonbStringMap(
            builder,
            static x => x.Settings,
            columnName: "settings"
        );

        _ = builder.Ignore(static x => x.DomainEvents);

        _ = builder.HasIndex(static x => new { x.ProviderKey, x.IsActive });
    }
}
