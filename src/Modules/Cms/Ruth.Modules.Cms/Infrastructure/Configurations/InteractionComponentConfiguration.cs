using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Personalization;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class InteractionComponentConfiguration
    : IEntityTypeConfiguration<InteractionComponent>
{
    public void Configure(EntityTypeBuilder<InteractionComponent> builder)
    {
        _ = builder.ToTable("interaction_components");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.ComponentKey).HasMaxLength(160).IsRequired();
        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static x => x.ComponentData,
            "component_data",
            createGinIndex: false
        );
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => new { x.InteractionChannelId, x.ComponentKey }).IsUnique();
    }
}
