using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Finance.Domain.Assistance;
using Ruth.Modules.Finance.Domain.Invoices;

namespace Ruth.Modules.Finance.Infrastructure.Configurations;

internal sealed class AssistanceDisbursementEntityConfiguration
    : IEntityTypeConfiguration<AssistanceDisbursement>
{
    public void Configure(EntityTypeBuilder<AssistanceDisbursement> builder)
    {
        _ = builder.ToTable("assistance_disbursements");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.CaseId).IsRequired();
        _ = builder
            .Property(static x => x.DisbursementMethod)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();
        _ = builder.Property(static x => x.Amount).HasPrecision(18, 3).IsRequired();
        _ = builder.Property(static x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        _ = builder.Property(static x => x.PayeeName).HasMaxLength(200).IsRequired();
        _ = builder
            .Property(static x => x.TargetInvoiceId)
            .HasConversion(
                static id => id.HasValue ? id.Value.Value : (Guid?)null,
                static guid => guid.HasValue ? InvoiceId.From(guid.Value) : null
            );
        _ = builder.Property(static x => x.ReferenceNumber).HasMaxLength(80);
        _ = builder.Property(static x => x.Notes).HasMaxLength(500);

        _ = builder.HasIndex(static x => x.CaseId);
        _ = builder.HasIndex(static x => x.TargetInvoiceId);
    }
}
