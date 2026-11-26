using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Finance.Domain.Recurring;

namespace Ruth.Modules.Finance.Infrastructure.Configurations;

internal sealed class ProcessedGatewayWebhookEntityConfiguration
    : IEntityTypeConfiguration<ProcessedGatewayWebhook>
{
    public void Configure(EntityTypeBuilder<ProcessedGatewayWebhook> builder)
    {
        _ = builder.ToTable("processed_gateway_webhooks");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.FinancialGatewayId).IsRequired();
        _ = builder.Property(static x => x.WebhookEventId).HasMaxLength(160).IsRequired();
        _ = builder
            .Property(static x => x.EventKind)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        _ = builder.HasIndex(static x => new { x.FinancialGatewayId, x.WebhookEventId }).IsUnique();
    }
}
