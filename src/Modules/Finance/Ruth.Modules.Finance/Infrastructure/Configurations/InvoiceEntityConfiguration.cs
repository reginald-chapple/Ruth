using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Finance.Domain.Invoices;

namespace Ruth.Modules.Finance.Infrastructure.Configurations;

internal sealed class InvoiceEntityConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        _ = builder.ToTable("invoices");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.InvoiceNumber).HasMaxLength(40).IsRequired();
        _ = builder.Property(static x => x.PersonId).IsRequired();
        _ = builder
            .Property(static x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder.Property(static x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        _ = builder.Property(static x => x.SubtotalAmount).HasPrecision(18, 3).IsRequired();
        _ = builder.Property(static x => x.PaidAmount).HasPrecision(18, 3).IsRequired();
        _ = builder.Property(static x => x.Memo).HasMaxLength(1000);

        _ = builder
            .HasMany(static x => x.LineItems)
            .WithOne()
            .HasForeignKey(static x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder
            .Navigation(static x => x.LineItems)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder.Ignore(static x => x.BalanceDue);
        _ = builder.Ignore(static x => x.DomainEvents);

        _ = builder.HasIndex(static x => x.InvoiceNumber).IsUnique();
        _ = builder.HasIndex(static x => new { x.PersonId, x.Status });
    }
}
