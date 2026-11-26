using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Finance.Domain.Assistance;

namespace Ruth.Modules.Finance.Infrastructure.Configurations;

internal sealed class AssistanceProgramEntityConfiguration
    : IEntityTypeConfiguration<AssistanceProgram>
{
    public void Configure(EntityTypeBuilder<AssistanceProgram> builder)
    {
        _ = builder.ToTable("assistance_programs");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.Code).HasMaxLength(64).IsRequired();
        _ = builder
            .Property(static x => x.Category)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();
        _ = builder.Property(static x => x.FundingAccountId).IsRequired();
        _ = builder.Property(static x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        _ = builder.Property(static x => x.TotalBudgetAmount).HasPrecision(18, 3).IsRequired();
        _ = builder.Property(static x => x.DisbursedAmount).HasPrecision(18, 3).IsRequired();
        _ = builder.Property(static x => x.MaxAwardPerCase).HasPrecision(18, 3).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(500);

        _ = builder.Ignore(static x => x.RemainingBudgetAmount);
        _ = builder.Ignore(static x => x.DomainEvents);

        _ = builder.HasIndex(static x => x.Code).IsUnique();
        _ = builder.HasIndex(static x => new { x.Category, x.IsActive });
    }
}
