using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Finance.Domain.Recurring;

namespace Ruth.Modules.Finance.Infrastructure.Configurations;

internal sealed class RecurringScheduleTransactionEntityConfiguration
    : IEntityTypeConfiguration<RecurringScheduleTransaction>
{
    public void Configure(EntityTypeBuilder<RecurringScheduleTransaction> builder)
    {
        _ = builder.ToTable("recurring_schedules");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.PersonId).IsRequired();
        _ = builder.Property(static x => x.FinancialGatewayId).IsRequired();
        _ = builder.Property(static x => x.SavedPaymentMethodId).IsRequired();
        _ = builder.Property(static x => x.GatewaySubscriptionReference).HasMaxLength(160);
        _ = builder
            .Property(static x => x.Purpose)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();
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
        _ = builder.Property(static x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        _ = builder.Property(static x => x.TotalAmount).HasPrecision(18, 3).IsRequired();
        _ = builder.Property(static x => x.LastFailureReason).HasMaxLength(400);

        _ = builder
            .HasMany(static x => x.Splits)
            .WithOne()
            .HasForeignKey(static x => x.ScheduleId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Splits)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder.Ignore(static x => x.DomainEvents);

        _ = builder.HasIndex(static x => new { x.Status, x.NextPaymentDate });
        _ = builder.HasIndex(static x => x.PersonId);
        _ = builder.HasIndex(static x => new
        {
            x.FinancialGatewayId,
            x.GatewaySubscriptionReference,
        });
    }
}
