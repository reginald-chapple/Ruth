using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Finance.Domain.Invoices;

namespace Ruth.Modules.Finance.Infrastructure.Configurations;

internal sealed class InvoiceLineItemEntityConfiguration : IEntityTypeConfiguration<InvoiceLineItem>
{
    public void Configure(EntityTypeBuilder<InvoiceLineItem> builder)
    {
        _ = builder.ToTable("invoice_line_items");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.InvoiceId).IsRequired();
        _ = builder.Property(static x => x.AccountId).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(300).IsRequired();
        _ = builder.Property(static x => x.Quantity).HasPrecision(18, 4).IsRequired();
        _ = builder.Property(static x => x.UnitAmount).HasPrecision(18, 3).IsRequired();
        _ = builder.Property(static x => x.LineTotal).HasPrecision(18, 3).IsRequired();

        _ = builder.HasIndex(static x => x.InvoiceId);
    }
}
