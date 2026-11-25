using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Crm.Domain.People;

namespace Ruth.Modules.Crm.Infrastructure.Configurations;

internal sealed class PersonSearchKeyConfiguration : IEntityTypeConfiguration<PersonSearchKey>
{
    public void Configure(EntityTypeBuilder<PersonSearchKey> builder)
    {
        _ = builder.ToTable("person_search_keys");
        _ = builder.HasKey(static k => k.Id);

        _ = builder.Property(static k => k.KeyType).HasConversion<string>().HasMaxLength(32);
        _ = builder.Property(static k => k.SearchValue).HasMaxLength(255).IsRequired();
        _ = builder.Property(static k => k.NormalizedSearchValue).HasMaxLength(255).IsRequired();

        _ = builder.HasIndex(static k => k.NormalizedSearchValue);
        _ = builder
            .HasIndex(static k => new
            {
                k.PersonId,
                k.KeyType,
                k.NormalizedSearchValue,
            })
            .IsUnique();
    }
}
