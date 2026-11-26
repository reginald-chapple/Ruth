using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Search.Domain.Collections;

namespace Ruth.Modules.Search.Infrastructure.Configurations;

internal sealed class ContentCollectionConfiguration
    : IEntityTypeConfiguration<ContentCollectionDefinition>
{
    public void Configure(EntityTypeBuilder<ContentCollectionDefinition> builder)
    {
        _ = builder.ToTable("content_collections");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(180).IsRequired();
        _ = builder.Property(static x => x.CollectionKey).HasMaxLength(96).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(1000);
        _ = builder
            .HasMany(static x => x.Sources)
            .WithOne()
            .HasForeignKey(static s => s.ContentCollectionId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Sources)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.CollectionKey).IsUnique();
    }
}
