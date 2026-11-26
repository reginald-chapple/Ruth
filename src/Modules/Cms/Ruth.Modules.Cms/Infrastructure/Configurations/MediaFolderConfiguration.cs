using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Media;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class MediaFolderConfiguration : IEntityTypeConfiguration<MediaFolder>
{
    public void Configure(EntityTypeBuilder<MediaFolder> builder)
    {
        _ = builder.ToTable("media_folders");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.ExternalFolderKey).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(1000);
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => new { x.MediaAccountId, x.ExternalFolderKey }).IsUnique();
    }
}
