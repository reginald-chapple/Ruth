using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.CadenceAlerts;

namespace Ruth.Modules.Finance.Infrastructure.Configurations;

internal sealed class TransactionCadenceAlertTypeEntityConfiguration
    : IEntityTypeConfiguration<TransactionCadenceAlertType>
{
    public void Configure(EntityTypeBuilder<TransactionCadenceAlertType> builder)
    {
        _ = builder.ToTable("cadence_alert_types");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.Name).HasMaxLength(120).IsRequired();
        _ = builder
            .Property(static x => x.AlertKind)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();
        _ = builder.Property(static x => x.MultiplierThreshold).HasPrecision(10, 3).IsRequired();
        _ = builder
            .Property(static x => x.StandardDeviationSensitivity)
            .HasPrecision(10, 3)
            .IsRequired();
        _ = builder.Property(static x => x.MinimumAmountThreshold).HasPrecision(18, 3).IsRequired();
        _ = builder
            .Property(static x => x.AccountId)
            .HasConversion(
                static id => id.HasValue ? id.Value.Value : (Guid?)null,
                static guid => guid.HasValue ? FinancialAccountId.From(guid.Value) : null
            );

        _ = builder.Ignore(static x => x.DomainEvents);
    }
}
