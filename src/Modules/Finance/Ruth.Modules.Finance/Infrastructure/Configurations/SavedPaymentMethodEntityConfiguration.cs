using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Finance.Domain.PaymentMethods;

namespace Ruth.Modules.Finance.Infrastructure.Configurations;

internal sealed class SavedPaymentMethodEntityConfiguration
    : IEntityTypeConfiguration<SavedPaymentMethod>
{
    public void Configure(EntityTypeBuilder<SavedPaymentMethod> builder)
    {
        _ = builder.ToTable("saved_payment_methods");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.PersonId).IsRequired();
        _ = builder.Property(static x => x.FinancialGatewayId).IsRequired();
        _ = builder.Property(static x => x.CustomerVaultToken).HasMaxLength(160).IsRequired();
        _ = builder
            .Property(static x => x.MethodKind)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder.Property(static x => x.DisplayName).HasMaxLength(120).IsRequired();
        _ = builder.Property(static x => x.CardBrand).HasMaxLength(64);
        _ = builder.Property(static x => x.AccountNumberMasked).HasMaxLength(32).IsRequired();

        _ = builder.Ignore(static x => x.DomainEvents);

        _ = builder.HasIndex(static x => new { x.PersonId, x.IsActive });
        _ = builder.HasIndex(static x => new { x.FinancialGatewayId, x.CustomerVaultToken });
    }
}
