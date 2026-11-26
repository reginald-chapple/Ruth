using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Contracts.Personalization;
using Ruth.Modules.Cms.Domain.Personalization;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class PersonalizationSegmentConfiguration
    : IEntityTypeConfiguration<PersonalizationSegment>
{
    public void Configure(EntityTypeBuilder<PersonalizationSegment> builder)
    {
        _ = builder.ToTable("personalization_segments");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.SegmentKey).HasMaxLength(128).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(1000);
        CmsJsonbConverters.ConfigureJsonbValue(
            builder,
            static x => x.FilterSpec,
            "filter_spec",
            static () => new PersonalizationSegmentFilterSpec()
        );
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.SegmentKey).IsUnique();
    }
}
