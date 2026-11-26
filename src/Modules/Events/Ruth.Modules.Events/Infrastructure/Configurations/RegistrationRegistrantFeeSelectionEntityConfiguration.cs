using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Events.Domain.Registrations;

namespace Ruth.Modules.Events.Infrastructure.Configurations;

internal sealed class RegistrationRegistrantFeeSelectionEntityConfiguration
    : IEntityTypeConfiguration<RegistrationRegistrantFeeSelection>
{
    public void Configure(EntityTypeBuilder<RegistrationRegistrantFeeSelection> builder)
    {
        _ = builder.ToTable("registration_registrant_fee_selections");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.RegistrantId).IsRequired();
        _ = builder.Property(static x => x.Label).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.UnitAmount).HasPrecision(18, 2).IsRequired();
        _ = builder.Property(static x => x.LineTotal).HasPrecision(18, 2).IsRequired();

        _ = builder.HasIndex(static x => x.RegistrantId);
        _ = builder.HasIndex(static x => x.FeeItemId);
    }
}
