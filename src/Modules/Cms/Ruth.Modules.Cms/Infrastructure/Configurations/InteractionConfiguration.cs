using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Personalization;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class InteractionConfiguration : IEntityTypeConfiguration<Interaction>
{
    public void Configure(EntityTypeBuilder<Interaction> builder)
    {
        _ = builder.ToTable("interactions");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Operation).HasMaxLength(64).IsRequired();
        _ = builder.Property(static x => x.Summary).HasMaxLength(500);
        _ = builder.Property(static x => x.SourceUrl).HasMaxLength(1024);
        _ = builder.Property(static x => x.ChannelCustom1).HasMaxLength(200);
        _ = builder.Property(static x => x.ChannelCustom2).HasMaxLength(200);
        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static x => x.InteractionData,
            "interaction_data",
            createGinIndex: false
        );
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => new { x.InteractionComponentId, x.OccurredDateTimeUtc });
        _ = builder.HasIndex(static x => new { x.PersonId, x.OccurredDateTimeUtc });
    }
}
