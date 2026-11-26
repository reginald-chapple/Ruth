using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Channels;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class SignageApplicationConfiguration : IEntityTypeConfiguration<SignageApplication>
{
    public void Configure(EntityTypeBuilder<SignageApplication> builder)
    {
        _ = builder.ToTable("signage_applications");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.Slug).HasMaxLength(96).IsRequired();
        _ = builder.Property(static x => x.Platform).HasConversion<string>().HasMaxLength(32);
        _ = builder.Property(static x => x.AdaptiveMessageKey).HasMaxLength(128);
        CmsJsonbConverters.ConfigureJsonbList(builder, static x => x.Slides, "slides");
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.Slug).IsUnique();
    }
}
