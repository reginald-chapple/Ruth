using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Finance.Domain.Campaigns;
using Ruth.Modules.Finance.Domain.Gateways;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Finance.Infrastructure.Configurations;

internal sealed class FundraisingCampaignEntityConfiguration
    : IEntityTypeConfiguration<FundraisingCampaign>
{
    public void Configure(EntityTypeBuilder<FundraisingCampaign> builder)
    {
        _ = builder.ToTable("fundraising_campaigns");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.Slug).HasMaxLength(96).IsRequired();
        _ = builder.Property(static x => x.GroupId).IsRequired();
        _ = builder.Property(static x => x.FinancialAccountId).IsRequired();
        _ = builder.Property(static x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        _ = builder.Property(static x => x.CampaignGoalAmount).HasPrecision(18, 3).IsRequired();
        _ = builder.Property(static x => x.TeamGoalOverrideAmount).HasPrecision(18, 3);
        _ = builder.Property(static x => x.DefaultParticipantGoalAmount).HasPrecision(18, 3);
        _ = builder.Property(static x => x.Summary).HasMaxLength(400);
        _ = builder.Property(static x => x.PublicDescription).HasMaxLength(4000);
        _ = builder
            .Property(static x => x.DefaultFinancialGatewayId)
            .HasConversion(
                static id => id.HasValue ? id.Value.Value : (Guid?)null,
                static guid => guid.HasValue ? FinancialGatewayId.From(guid.Value) : null
            );
        _ = builder
            .Property(static x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static x => x.AttributeValues,
            columnName: "attribute_values",
            createGinIndex: true
        );

        _ = builder
            .HasMany(static x => x.Participants)
            .WithOne()
            .HasForeignKey(static x => x.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Participants)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder
            .HasMany(static x => x.Credits)
            .WithOne()
            .HasForeignKey(static x => x.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Credits)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder.Ignore(static x => x.EffectiveTeamGoalAmount);
        _ = builder.Ignore(static x => x.DomainEvents);

        _ = builder.HasIndex(static x => x.Slug).IsUnique();
        _ = builder.HasIndex(static x => new { x.GroupId, x.Status });
        _ = builder.HasIndex(static x => x.FinancialAccountId);
    }
}
