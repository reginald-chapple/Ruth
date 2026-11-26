using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Events.Domain.Registrations;

namespace Ruth.Modules.Events.Infrastructure.Configurations;

internal sealed class RegistrationPaymentRecordEntityConfiguration
    : IEntityTypeConfiguration<RegistrationPaymentRecord>
{
    public void Configure(EntityTypeBuilder<RegistrationPaymentRecord> builder)
    {
        _ = builder.ToTable("registration_payment_records");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.RegistrationId).IsRequired();
        _ = builder.Property(static x => x.FinancialTransactionId).IsRequired();
        _ = builder.Property(static x => x.Amount).HasPrecision(18, 2).IsRequired();
        _ = builder.Property(static x => x.Note).HasMaxLength(500);

        _ = builder.HasIndex(static x => x.RegistrationId);
    }
}
