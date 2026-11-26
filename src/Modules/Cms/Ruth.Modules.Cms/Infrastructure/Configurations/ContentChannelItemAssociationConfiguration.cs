using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Content;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class ContentChannelItemAssociationConfiguration
    : IEntityTypeConfiguration<ContentChannelItemAssociation>
{
    public void Configure(EntityTypeBuilder<ContentChannelItemAssociation> builder)
    {
        _ = builder.ToTable("content_channel_item_associations");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.HasIndex(static x => new { x.ParentItemId, x.ChildItemId }).IsUnique();
    }
}
