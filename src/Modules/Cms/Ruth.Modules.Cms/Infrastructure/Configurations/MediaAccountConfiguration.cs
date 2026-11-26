using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Media;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class MediaAccountConfiguration : IEntityTypeConfiguration<MediaAccount>
{
    public void Configure(EntityTypeBuilder<MediaAccount> builder)
    {
        _ = builder.ToTable("media_accounts");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.Slug).HasMaxLength(96).IsRequired();
        _ = builder.Property(static x => x.ProviderKey).HasMaxLength(64).IsRequired();
        _ = builder.Property(static x => x.AccountIdentifier).HasMaxLength(200).IsRequired();
        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static x => x.Settings,
            "settings",
            createGinIndex: false
        );
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.Slug).IsUnique();
    }
}
