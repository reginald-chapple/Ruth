using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Finance.Domain.Campaigns;

namespace Ruth.Modules.Finance.Infrastructure.Configurations;

internal sealed class CampaignParticipantEntityConfiguration
    : IEntityTypeConfiguration<CampaignParticipant>
{
    public void Configure(EntityTypeBuilder<CampaignParticipant> builder)
    {
        _ = builder.ToTable("campaign_participants");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.CampaignId).IsRequired();
        _ = builder.Property(static x => x.PersonId).IsRequired();
        _ = builder.Property(static x => x.DisplayName).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.CustomGoalAmount).HasPrecision(18, 3);
        _ = builder.Property(static x => x.PersonalMessage).HasMaxLength(500);

        _ = builder.HasIndex(static x => new { x.CampaignId, x.PersonId }).IsUnique();
    }
}
