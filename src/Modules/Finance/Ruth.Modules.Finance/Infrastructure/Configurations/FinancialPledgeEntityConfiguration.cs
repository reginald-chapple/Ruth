using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Finance.Domain.Pledges;

namespace Ruth.Modules.Finance.Infrastructure.Configurations;

internal sealed class FinancialPledgeEntityConfiguration : IEntityTypeConfiguration<FinancialPledge>
{
    public void Configure(EntityTypeBuilder<FinancialPledge> builder)
    {
        _ = builder.ToTable("financial_pledges");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.PersonId).IsRequired();
        _ = builder.Property(static x => x.AccountId).IsRequired();
        _ = builder.Property(static x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        _ = builder.Property(static x => x.TotalAmount).HasPrecision(18, 3).IsRequired();
        _ = builder
            .Property(static x => x.Frequency)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder
            .Property(static x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder.Property(static x => x.Notes).HasMaxLength(1000);

        _ = builder.Ignore(static x => x.DomainEvents);

        _ = builder.HasIndex(static x => x.PersonId);
        _ = builder.HasIndex(static x => new { x.AccountId, x.Status });
    }
}
