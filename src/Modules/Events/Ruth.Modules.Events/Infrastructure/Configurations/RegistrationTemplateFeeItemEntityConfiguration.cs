using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Events.Domain.Registrations;

namespace Ruth.Modules.Events.Infrastructure.Configurations;

internal sealed class RegistrationTemplateFeeItemEntityConfiguration
    : IEntityTypeConfiguration<RegistrationTemplateFeeItem>
{
    public void Configure(EntityTypeBuilder<RegistrationTemplateFeeItem> builder)
    {
        _ = builder.ToTable("registration_template_fee_items");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.FeeId).IsRequired();
        _ = builder.Property(static x => x.Code).HasMaxLength(64).IsRequired();
        _ = builder.Property(static x => x.Label).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.UnitAmount).HasPrecision(18, 2).IsRequired();

        _ = builder.HasIndex(static x => new { x.FeeId, x.DisplayOrder });
    }
}
