using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Datasets;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class PersistedDatasetConfiguration : IEntityTypeConfiguration<PersistedDataset>
{
    public void Configure(EntityTypeBuilder<PersistedDataset> builder)
    {
        _ = builder.ToTable("persisted_datasets");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.AccessKey).HasMaxLength(128).IsRequired();
        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(1000);
        _ = builder.Property(static x => x.DataSourceKind).HasConversion<string>().HasMaxLength(40);
        _ = builder.Property(static x => x.ResultDataJson).HasColumnType("jsonb");
        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static x => x.SourceConfiguration,
            "source_configuration",
            createGinIndex: false
        );
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.AccessKey).IsUnique();
    }
}
