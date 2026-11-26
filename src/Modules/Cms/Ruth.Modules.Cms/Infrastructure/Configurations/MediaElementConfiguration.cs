using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Media;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class MediaElementConfiguration : IEntityTypeConfiguration<MediaElement>
{
    public void Configure(EntityTypeBuilder<MediaElement> builder)
    {
        _ = builder.ToTable("media_elements");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(240).IsRequired();
        _ = builder.Property(static x => x.ExternalElementKey).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(2000);
        _ = builder.Property(static x => x.ThumbnailUrl).HasMaxLength(1024);
        _ = builder.Property(static x => x.PlaybackUrl).HasMaxLength(1024);
        _ = builder.Property(static x => x.HlsManifestUrl).HasMaxLength(1024);
        _ = builder.Property(static x => x.CaptionsUrl).HasMaxLength(1024);
        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static x => x.Metadata,
            "metadata",
            createGinIndex: false
        );
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => new { x.MediaFolderId, x.ExternalElementKey }).IsUnique();
    }
}
