using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Bookmarks;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class PersonalLinkConfiguration : IEntityTypeConfiguration<PersonalLink>
{
    public void Configure(EntityTypeBuilder<PersonalLink> builder)
    {
        _ = builder.ToTable("personal_links");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.Url).HasMaxLength(2048).IsRequired();
        _ = builder.Property(static x => x.IconName).HasMaxLength(80);
        _ = builder.HasIndex(static x => x.SectionId);
    }
}
