using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Finance.Domain.PaymentMethods;

namespace Ruth.Modules.Finance.Infrastructure.Configurations;

internal sealed class BankAccountIdentifierEntityConfiguration
    : IEntityTypeConfiguration<BankAccountIdentifier>
{
    public void Configure(EntityTypeBuilder<BankAccountIdentifier> builder)
    {
        _ = builder.ToTable("bank_account_identifiers");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.PersonId).IsRequired();
        _ = builder.Property(static x => x.RoutingAndAccountHash).HasMaxLength(64).IsRequired();
        _ = builder.Property(static x => x.MaskedRoutingNumber).HasMaxLength(16).IsRequired();
        _ = builder.Property(static x => x.MaskedAccountNumber).HasMaxLength(24).IsRequired();
        _ = builder.Property(static x => x.BankName).HasMaxLength(120);

        _ = builder.Ignore(static x => x.DomainEvents);

        _ = builder.HasIndex(static x => x.RoutingAndAccountHash).IsUnique();
        _ = builder.HasIndex(static x => x.PersonId);
    }
}
