using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Crm.Domain.People;

namespace Ruth.Modules.Crm.Infrastructure.Configurations;

internal sealed class MetaFirstNameGenderLookupConfiguration
    : IEntityTypeConfiguration<MetaFirstNameGenderLookup>
{
    public void Configure(EntityTypeBuilder<MetaFirstNameGenderLookup> builder)
    {
        _ = builder.ToTable("meta_first_name_gender_lookups");
        _ = builder.HasKey(static m => m.Id);

        _ = builder.Property(static m => m.FirstName).HasMaxLength(50).IsRequired();
        _ = builder.Property(static m => m.NormalizedFirstName).HasMaxLength(50).IsRequired();
        _ = builder.Property(static m => m.Country).HasMaxLength(10).IsRequired();
        _ = builder.Property(static m => m.Language).HasMaxLength(10).IsRequired();
        _ = builder.Property(static m => m.MalePercent).HasPrecision(5, 2);
        _ = builder.Property(static m => m.FemalePercent).HasPrecision(5, 2);

        _ = builder.HasIndex(static m => m.NormalizedFirstName);
    }
}
