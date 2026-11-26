using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Finance.Domain.Campaigns;

namespace Ruth.Modules.Finance.Infrastructure.Configurations;

internal sealed class CampaignContributionCreditEntityConfiguration
    : IEntityTypeConfiguration<CampaignContributionCredit>
{
    public void Configure(EntityTypeBuilder<CampaignContributionCredit> builder)
    {
        _ = builder.ToTable("campaign_contribution_credits");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.CampaignId).IsRequired();
        _ = builder.Property(static x => x.TransactionId).IsRequired();
        _ = builder
            .Property(static x => x.CreditedParticipantId)
            .HasConversion(
                static id => id.HasValue ? id.Value.Value : (Guid?)null,
                static guid => guid.HasValue ? CampaignParticipantId.From(guid.Value) : null
            );
        _ = builder.Property(static x => x.DonorDisplayName).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.Amount).HasPrecision(18, 3).IsRequired();
        _ = builder.Property(static x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        _ = builder
            .Property(static x => x.DonorPrivacy)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder.Property(static x => x.DonorComment).HasMaxLength(500);

        _ = builder.HasIndex(static x => x.CampaignId);
        _ = builder.HasIndex(static x => x.TransactionId);
        _ = builder.HasIndex(static x => x.CreditedParticipantId);
        _ = builder.HasIndex(static x => x.DonorPersonId);
    }
}
