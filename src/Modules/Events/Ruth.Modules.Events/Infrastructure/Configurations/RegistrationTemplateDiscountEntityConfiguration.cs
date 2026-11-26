using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Events.Domain.Registrations;

namespace Ruth.Modules.Events.Infrastructure.Configurations;

internal sealed class RegistrationTemplateDiscountEntityConfiguration
    : IEntityTypeConfiguration<RegistrationTemplateDiscount>
{
    public void Configure(EntityTypeBuilder<RegistrationTemplateDiscount> builder)
    {
        _ = builder.ToTable("registration_template_discounts");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.TemplateId).IsRequired();
        _ = builder.Property(static x => x.Code).HasMaxLength(48).IsRequired();
        _ = builder
            .Property(static x => x.DiscountKind)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder.Property(static x => x.DiscountValue).HasPrecision(18, 2).IsRequired();

        _ = builder.HasIndex(static x => new { x.TemplateId, x.Code }).IsUnique();
    }
}
