using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Finance.Domain.CadenceAlerts;

namespace Ruth.Modules.Finance.Infrastructure.Configurations;

internal sealed class TransactionCadenceAlertEntityConfiguration
    : IEntityTypeConfiguration<TransactionCadenceAlert>
{
    public void Configure(EntityTypeBuilder<TransactionCadenceAlert> builder)
    {
        _ = builder.ToTable("cadence_alerts");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.AlertTypeId).IsRequired();
        _ = builder
            .Property(static x => x.AlertKind)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();
        _ = builder.Property(static x => x.PersonId).IsRequired();
        _ = builder
            .Property(static x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder.Property(static x => x.BaselineMeanAmount).HasPrecision(18, 3).IsRequired();
        _ = builder.Property(static x => x.BaselineIntervalDays).HasPrecision(18, 3).IsRequired();
        _ = builder.Property(static x => x.ObservedValue).HasPrecision(18, 3).IsRequired();
        _ = builder.Property(static x => x.Summary).HasMaxLength(500).IsRequired();
        _ = builder.Property(static x => x.ResolutionNotes).HasMaxLength(500);

        _ = builder.Ignore(static x => x.DomainEvents);

        _ = builder.HasIndex(static x => new
        {
            x.PersonId,
            x.AlertTypeId,
            x.DetectedOnUtc,
        });
        _ = builder.HasIndex(static x => new { x.Status, x.AlertKind });
    }
}
